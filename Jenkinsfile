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
                  > trivy-report.txt 2>&1
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
                  --batch > sqlmap-report.txt 2>&1
        
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
                        -r zap-report.html \
						> zap-report.txt 2>&1
        
                    ZAP_EXIT=$?
					
					cat zap-report.txt
        
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
		
		stage('Security Gate') {
            steps {
                sh '''
                    FAILED=false
        
                    echo "================================"
                    echo "       SECURITY GATE"
                    echo "================================"
        
                    # 1. Semgrep
                    if [ ! -f semgrep-report.txt ]; then
                        echo "❌ Semgrep report missing"
                        FAILED=true
                    elif grep -Eqi "Findings:[[:space:]]*[1-9][0-9]*|[1-9][0-9]* blocking" semgrep-report.txt; then
                        echo "❌ Semgrep detected findings"
                        FAILED=true
                    else
                        echo "✅ Semgrep clean"
                    fi
        
                    # 2. TruffleHog
                    if [ ! -f trufflehog-report.txt ]; then
                        echo "❌ TruffleHog report missing"
                        FAILED=true
                    elif grep -Eqi '"verified_secrets":[[:space:]]*[1-9]|"unverified_secrets":[[:space:]]*[1-9]|Found verified result|Found unverified result' trufflehog-report.txt; then
                        echo "❌ TruffleHog detected secrets"
                        FAILED=true
                    else
                        echo "✅ TruffleHog clean"
                    fi
        
                    # 3. SQLMap
                    if [ ! -f sqlmap-report.txt ]; then
                        echo "❌ SQLMap report missing"
                        FAILED=true
                    elif grep -Eqi "parameter.*is injectable|identified the following injection point|sqlmap identified.*injectable" sqlmap-report.txt; then
                        echo "❌ SQL Injection detected"
                        FAILED=true
                    elif grep -Eqi "unable to connect|connection refused|host.*not known|no such host" sqlmap-report.txt; then
                        echo "❌ SQLMap could not reach the application"
                        FAILED=true
                    else
                        echo "✅ SQLMap clean"
                    fi
        
                    # 4. Trivy
                    if [ ! -f trivy-report.txt ]; then
                        echo "❌ Trivy report missing"
                        FAILED=true
                    elif grep -Eqi "CRITICAL:[[:space:]]*[1-9][0-9]*" trivy-report.txt; then
                        echo "❌ Trivy detected CRITICAL vulnerabilities"
                        FAILED=true
                    else
                        echo "✅ Trivy: no blocking CRITICAL findings"
                    fi
        
                    # 5. OWASP ZAP
                    if [ ! -f zap-reports/zap-report.html ]; then
                        echo "❌ ZAP report missing"
                        FAILED=true
                    elif grep -Eqi "FAIL-NEW:[[:space:]]*[1-9][0-9]*|FAIL-INPROG:[[:space:]]*[1-9][0-9]*" zap-reports/zap-report.html; then
                        echo "❌ OWASP ZAP detected blocking findings"
                        FAILED=true
                    else
                        echo "✅ OWASP ZAP completed without blocking failures"
                    fi
        
                    echo ""
                    echo "================================"
        
                    if [ "$FAILED" = true ]; then
                        echo "       SECURITY GATE FAILED"
                        echo "================================"
                        exit 1
                    fi
        
                    echo "       SECURITY GATE PASSED"
                    echo "================================"
                '''
            }
        }
    }

    post {
        always {
            archiveArtifacts(
                artifacts: 'semgrep-report.txt,trufflehog-report.txt,trivy-report.txt,sqlmap-report.txt,zap-report.html,zap-reports/*.html',
                allowEmptyArchive: true,
                fingerprint: true
            )
        }
    
        success {
            echo 'DevSecOps pipeline completed successfully.'
        }
    
        failure {
            echo 'DevSecOps pipeline failed. Check the security reports.'
        }
    }
}