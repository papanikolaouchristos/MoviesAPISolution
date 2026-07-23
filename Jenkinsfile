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
                        --volumes-from jenkins \
                        semgrep/semgrep \
                        semgrep scan \
                        --config /var/jenkins_home/workspace/MoviesAPI-Security-Pipeline/semgrep-rules \
                        --no-git-ignore \
                        --include="*.cs" \
                        /var/jenkins_home/workspace/MoviesAPI-Security-Pipeline \
                        > semgrep-report.txt 2>&1
        
                    cat semgrep-report.txt
                '''
            }
        }

        stage('TruffleHog') {
            steps {
                sh '''
                    docker run --rm \
                        --volumes-from jenkins \
                        trufflesecurity/trufflehog:latest \
                        filesystem \
                        /var/jenkins_home/workspace/MoviesAPI-Security-Pipeline \
                        --no-update \
                        > trufflehog-report.txt 2>&1
        
                    cat trufflehog-report.txt
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
                    docker compose down --remove-orphans || true
                    docker compose up -d
        
                    echo "Waiting for MoviesAPI..."
        
                    for i in $(seq 1 30); do
                        if docker run --rm \
                            --network moviesapi-security-pipeline_default \
                            curlimages/curl:latest \
                            -fsS http://moviesapi:8080/swagger/v1/swagger.json \
                            > /dev/null
                        then
                            echo "MoviesAPI is ready"
                            exit 0
                        fi
        
                        sleep 2
                    done
        
                    echo "MoviesAPI did not become ready"
                    docker compose logs moviesapi
                    exit 1
                '''
            }
        }

        stage('SQLMap') {
            steps {
                sh '''
                docker run --rm \
                  --network moviesapi-security-pipeline_default \
                  --volumes-from jenkins \
                  parrotsec/sqlmap \
                  -m /var/jenkins_home/workspace/MoviesAPI-Security-Pipeline/Targets/endpoints-jenkins.txt \
                  --batch > sqlmap-report.txt
        
                cat sqlmap-report.txt
                '''
            }
        }

        stage('OWASP ZAP') {
            steps {
                sh '''
                    rm -rf zap-reports
                    mkdir -p zap-reports
                    chmod 777 zap-reports
        
                    set +e
        
                    docker run --rm \
                        --user root \
                        --network moviesapi-security-pipeline_default \
                        -v "$(pwd)/zap-reports:/zap/wrk:rw" \
                        ghcr.io/zaproxy/zaproxy:stable \
                        zap-baseline.py \
                        -t http://moviesapi:8080/swagger/index.html \
                        -r zap-report.html
        
                    ZAP_EXIT=$?
        
                    set -e
        
                    if [ -f zap-reports/zap-report.html ]; then
                        cp zap-reports/zap-report.html ./zap-report.html
                    fi
        
                    if [ "$ZAP_EXIT" -eq 0 ]; then
                        echo "OWASP ZAP completed successfully"
                    elif [ "$ZAP_EXIT" -eq 2 ]; then
                        echo "OWASP ZAP completed with warnings"
                    elif [ "$ZAP_EXIT" -eq 1 ]; then
                        echo "OWASP ZAP detected blocking findings"
                        exit 1
                    else
                        echo "OWASP ZAP execution failed with exit code $ZAP_EXIT"
                        exit "$ZAP_EXIT"
                    fi
                '''
            }
        }
    }

    post {
        always {
            archiveArtifacts artifacts: '*.txt,*.html,zap-reports/*.html',
                             allowEmptyArchive: true,
                             fingerprint: true
        }
    }
}