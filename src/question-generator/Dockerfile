FROM python:3.11-slim

WORKDIR /app

# Copy requirements
COPY requirements.txt .
# Add fastapi and uvicorn to requirements or install directly
RUN pip install --no-cache-dir -r requirements.txt && \
    pip install --no-cache-dir fastapi uvicorn

# Copy source code
COPY . .

# Set python path
ENV PYTHONPATH=/app

# Expose port
EXPOSE 8000

# Run the API
CMD ["python", "src/api.py"]
