import sys
from pathlib import Path

from fastapi.testclient import TestClient

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from main import app


def test_health_and_irt_estimate_http_contract():
    with TestClient(app) as client:
        assert client.get('/health').json()['status'] == 'ok'
        result = client.post('/irt/estimate', json={'responses': [
            {'question_id': 1, 'is_correct': True, 'irt_params': {'a': 1, 'b': 0, 'c': 0.25}}
        ], 'method': 'EAP'})
        assert result.status_code == 200
        assert result.json()['estimate']['theta'] > 0


def test_profile_http_preserves_string_subject_and_domain():
    with TestClient(app) as client:
        response = client.post('/profile/generate', json={
            'user_id': 'candidate-guid', 'session_id': 'session', 'theta': 0, 'se_theta': 1,
            'skill_masteries': [{'skill_id': 2, 'skill_name': 'Data', 'domain_id': 2,
                'domain_name': 'Data', 'mastery_prob': 0.7, 'weight': 1}]
        })
        assert response.status_code == 200
        assert response.json()['user_id'] == 'candidate-guid'
        assert response.json()['domain_scores'][0]['domain_name'] == 'Data'
