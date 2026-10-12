#!/usr/bin/env python3
"""Exercise the local compose stack with synthetic accounts and real ML inference."""
import argparse
import json
import time
import urllib.error
import urllib.request
import urllib.parse


def request(base, path, data=None, token=None, form=False):
    headers = {}
    if token:
        headers['Authorization'] = f'Bearer {token}'
    body = None
    if data is not None:
        headers['Content-Type'] = 'application/x-www-form-urlencoded' if form else 'application/json'
        body = urllib.parse.urlencode(data).encode() if form else json.dumps(data).encode()
    req = urllib.request.Request(base + path, data=body, headers=headers)
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
    for group in framework['domains']:
        status, _ = request(args.api, '/api/questions', {
            'title': 'Smoke assessment', 'prompt': 'Choose A', 'type': 'mcq_single',
            'competencyGroupId': group['name'], 'competencyLevelId': 'Basic', 'difficulty': 2,
            'options': ['A. Correct', 'B. Incorrect'], 'answer': 'A', 'status': 'published'
        }, admin)
        assert status == 201
    accounts = []
    for name in ['alice', 'bob']:
        email = f'{name}-phase0@example.com'
        assert request(args.api, '/api/account/register', {'email': email, 'password': 'Password123!'})[0] == 201
        token = login(args.api, email)
        assert request(args.api, '/api/questions', token=token)[0] == 403
        assert request(args.ui, '/api/framework/active', token=token)[0] == 200
        status, started = request(args.api, '/api/candidate/tests/start-standard', {}, token)
        assert status == 200
        accounts.append((token, started['sessionId']))
    alice, bob = accounts
    assert alice[1] != bob[1]
    assert request(args.api, f'/api/candidate/tests/sessions/{alice[1]}', token=bob[0])[0] == 404
    for index, (token, session_id) in enumerate(accounts):
        status, session = request(args.api, f'/api/candidate/tests/sessions/{session_id}', token=token)
        assert status == 200 and len(session['questions']) == 7
        assert all('answer' not in q['metadata'] for q in session['questions'])
        answers = [{'questionId': q['id'], 'answer': 'A. Correct' if index == 0 else 'B. Incorrect'} for q in session['questions']]
        assert request(args.api, f'/api/candidate/tests/sessions/{session_id}/submit', {'responses': answers}, token)[0] == 200
        status, result = request(args.api, f'/api/candidate/tests/sessions/{session_id}/result', token=token)
        assert status == 200 and result['totalScore'] == (7 if index == 0 else 0)
        assert result['passScore'] == 5 and result['passed'] == (index == 0)
        status, history = request(args.api, '/api/candidate/tests/history', token=token)
        assert status == 200 and len(history) == 1 and history[0]['sessionId'] == session_id
        for attempt in range(40):
            status, profile = request(args.api, '/api/competency/profile', token=token)
            if status == 200:
                break
            time.sleep(0.5)
        assert status == 200 and len(profile['domainScores']) == 7
        assert profile['sessionId'] == session_id
    print('Compose acceptance passed: all service health checks, UI/proxy, real token authorization, two isolated candidates, scoring, seven-domain ML profiles.')


if __name__ == '__main__':
    main()
