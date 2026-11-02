FROM python:3.11-slim
WORKDIR /app/src/question-generator
COPY src/question-generator/requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt && pip install --no-cache-dir fastapi uvicorn
COPY src/question-generator/ .
COPY data/frameworks/ /app/data/frameworks/
ENV PYTHONPATH=/app/src/question-generator
EXPOSE 8000
CMD ["python", "src/api.py"]
