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
                docker run --rm \
                -v $(pwd):/src \
                semgrep/semgrep \
                semgrep scan --config auto --no-git-ignore /src
                '''
            }
        }

        stage('TruffleHog') {
            steps {
                sh '''
                docker run --rm \
                -v /var/jenkins_home/workspace/MoviesAPI-Security-Pipeline:/repo \
                trufflesecurity/trufflehog:latest filesystem /repo --no-update
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
                  > trivy-report.txt 
                '''
            }
        }

        stage('Run Application') {
            steps {
                sh '''
                    docker rm -f moviesapi || true
                    docker compose down --remove-orphans
                    docker compose up -d
                '''
            }
        }

        stage('SQLMap') {
            steps {
                sh '''
                docker run --rm \
                --network moviesapi-security-pipeline_default \
                secunit/sqlmap \
                -u "http://moviesapi:8080/api/movies/search?title=test" \
                --batch
                '''
            }
        }

        stage('OWASP ZAP') {
            steps {
                sh '''
                chmod -R 777 .
        
                docker run --rm \
                --network moviesapi-security-pipeline_default \
                -v $(pwd):/zap/wrk \
                ghcr.io/zaproxy/zaproxy:stable \
                zap-baseline.py \
                -t http://moviesapi:8080 \
                -r zap-report.html
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