#!/usr/bin/env python3
"""Exercise the local compose stack with synthetic accounts and real CTT inference."""
import argparse
import json
import urllib.error
import urllib.request
import urllib.parse


def request(base, path, data=None, token=None, form=False, method=None):
    headers = {}
    if token:
        headers['Authorization'] = f'Bearer {token}'
    body = None
    if data is not None:
        headers['Content-Type'] = 'application/x-www-form-urlencoded' if form else 'application/json'
        body = urllib.parse.urlencode(data).encode() if form else json.dumps(data).encode()
    req = urllib.request.Request(base + path, data=body, headers=headers, method=method)
    try:
        with urllib.request.urlopen(req, timeout=20) as response:
            content = response.read().decode()
            try:
                value = json.loads(content) if content else None
            except json.JSONDecodeError:
                value = content
            return response.status, value
    except urllib.error.HTTPError as error:
        return error.code, None


def login(base, email):
    status, body = request(base, '/connect/token', {
        'grant_type': 'password', 'client_id': 'ai-competency-ui', 'username': email,
        'password': 'Password123!', 'scope': 'openid profile email roles'
    }, form=True)
    assert status == 200, f'Login returned {status}'
    return body['access_token']


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--api', required=True)
    parser.add_argument('--ui', required=True)
    parser.add_argument('--ml', required=True)
    parser.add_argument('--generator', required=True)
    args = parser.parse_args()
    for base in [args.api, args.ml, args.generator]:
        assert request(base, '/health')[0] == 200
    with urllib.request.urlopen(args.ui + '/candidate/register') as page:
        assert 'app-root' in page.read().decode()
    admin = login(args.api, 'admin@example.com')
    status, framework = request(args.api, '/api/framework/active', token=admin)
    assert status == 200 and len(framework['domains']) == 7
    status, features = request(args.api, '/api/features')
    assert status == 200 and features['adaptiveTesting'] is False
    # Phase 2: questions are reviewed before publication; a standard form takes 5 per group.
    for group in framework['domains']:
        for level in ['Basic', 'Basic', 'Intermediate', 'Intermediate', 'Advanced']:
            status, question = request(args.api, '/api/questions', {
                'title': f"Smoke {group['code']} {level}", 'prompt': 'Choose A', 'type': 'mcq_single',
                'competencyGroupId': group['name'], 'competencyLevelId': level, 'difficulty': 2,
                'options': ['A. Correct', 'B. Incorrect'], 'answer': 'A', 'status': 'draft'
            }, admin)
            assert status == 201
            assert request(args.api, f"/api/questions/{question['id']}/review", {'note': 'Smoke review'}, admin)[0] == 200
            assert request(args.api, f"/api/questions/{question['id']}", {**question, 'status': 'published'}, admin, method='PUT')[0] == 200
    status, form = request(args.api, '/api/tests', {
        'name': 'Smoke standard form', 'isStandard': True, 'seed': 7, 'passPercent': 70,
        'groupQuotas': {group['code']: 5 for group in framework['domains']}
    }, admin)
    assert status == 201 and len(form['questions']) == 35 and form['standardIssues'] == []
    status, published = request(args.api, f"/api/tests/{form['id']}/publish", {}, admin)
    assert status == 200 and published['status'] == 'active' and published['passingScore'] == 25
    accounts = []
    for name in ['alice', 'bob']:
        email = f'{name}-phase2@example.com'
        assert request(args.api, '/api/account/register', {'email': email, 'password': 'Password123!'})[0] == 201
        token = login(args.api, email)
        assert request(args.api, '/api/questions', token=token)[0] == 403
        assert request(args.ui, '/api/framework/active', token=token)[0] == 200
        assert request(args.api, '/api/adaptivetests/start', {}, token)[0] == 404
        status, started = request(args.api, '/api/candidate/tests/start-standard', {}, token)
        assert status == 200
        accounts.append((token, started['sessionId']))
    alice, bob = accounts
    assert alice[1] != bob[1]
    assert request(args.api, f'/api/candidate/tests/sessions/{alice[1]}', token=bob[0])[0] == 404
    for index, (token, session_id) in enumerate(accounts):
        status, session = request(args.api, f'/api/candidate/tests/sessions/{session_id}', token=token)
        assert status == 200 and len(session['questions']) == 35
        assert all('answer' not in q['metadata'] for q in session['questions'])
        answers = [{'questionId': q['id'], 'answer': 'A. Correct' if index == 0 else 'B. Incorrect'} for q in session['questions']]
        assert request(args.api, f'/api/candidate/tests/sessions/{session_id}/submit', {'responses': answers}, token)[0] == 200
        status, result = request(args.api, f'/api/candidate/tests/sessions/{session_id}/result', token=token)
        assert status == 200 and result['totalScore'] == (35 if index == 0 else 0)
        assert result['passScore'] == 25 and result['passed'] == (index == 0)
        assert [g['percent'] for g in result['groupScores']] == [100 if index == 0 else 0] * 7
        status, history = request(args.api, '/api/candidate/tests/history', token=token)
        assert status == 200 and len(history) == 1 and history[0]['sessionId'] == session_id
    # The level rule is fixed by approval; earlier group scores receive levels from it.
    status, mapping = request(args.api, '/api/test-level-mappings/tl-v1-draft/publish',
                              {'reviewedBy': 'Smoke validation', 'reviewNotes': 'Synthetic data'}, admin)
    assert status == 200 and mapping['backfilledGroupScores'] == 14
    for index, (token, session_id) in enumerate(accounts):
        status, profile = request(args.api, '/api/competency/test-profile', token=token)
        assert status == 200 and profile['sessionId'] == session_id
        assert {g['levelCode'] for g in profile['groups']} == {'ADVANCED' if index == 0 else 'BASIC'}
    # Real CTT inference through the API and ML service.
    status, analysis = request(args.api, f"/api/tests/{form['id']}/item-analysis", token=admin)
    assert status == 200 and analysis['analysis']['nRespondents'] == 2 and analysis['analysis']['nItems'] == 35
    assert analysis['analysis']['reliability']['alpha'] is not None
    status, summary = request(args.api, f"/api/reports/summary?examId={form['id']}", token=admin)
    assert status == 200 and summary['attempts'] == 2 and summary['passRate'] == 50
    print('Compose acceptance passed: service health, UI/proxy, real token authorization, reviewed questions, '
          'standard form publication, two isolated candidates, seven-group scores and levels, CTT via the ML service, reports.')


if __name__ == '__main__':
    main()
