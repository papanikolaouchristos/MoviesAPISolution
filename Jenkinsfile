pipeline {

    agent any

    stages {

        stage('Checkout') {
            steps {
                checkout scm
            }
        }

        stage('Semgrep') {
            steps {
                sh '''
                semgrep \
                  --config semgrep-rules \
                  . > semgrep-report.txt || true
                '''
            }
        }

        stage('TruffleHog') {
            steps {
                sh '''
                docker run --rm \
                  -v $PWD:/repo \
                  trufflesecurity/trufflehog:latest \
                  filesystem /repo \
                  --no-update \
                  > trufflehog-report.txt || true
                '''
            }
        }

        stage('Build Image') {
            steps {
                sh 'docker build -t moviesapi-sec .'
            }
        }

        stage('Trivy') {
            steps {
                sh '''
                docker run --rm \
                  -v /var/run/docker.sock:/var/run/docker.sock \
                  aquasec/trivy image \
                  moviesapi-sec \
                  > trivy-report.txt || true
                '''
            }
        }

        stage('Run Application') {
            steps {
                sh '''
                docker compose down || true
                docker compose up -d
                sleep 20
                '''
            }
        }

        stage('SQLMap') {
            steps {
                sh '''
                python3 sqlmap/sqlmap.py \
                  -u "http://moviesapi:8080/api/movies/search?title=test" \
                  --batch \
                  > sqlmap-report.txt || true
                '''
            }
        }

        stage('OWASP ZAP') {
            steps {
                sh '''
                docker run --rm \
                  --network moviesapi_default \
                  -v $PWD:/zap/wrk \
                  ghcr.io/zaproxy/zaproxy:stable \
                  zap-baseline.py \
                  -t http://moviesapi:8080 \
                  -r zap-report.html || true
                '''
            }
        }
    }

    post {

        always {

            archiveArtifacts artifacts: '*.txt,*.html', fingerprint: true

        }
    }
}