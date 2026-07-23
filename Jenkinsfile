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
                    rm -f trufflehog-report.txt
        
                    docker run --rm \
                        --volumes-from jenkins \
                        trufflesecurity/trufflehog:latest \
                        filesystem /var/jenkins_home/workspace/MoviesAPI-Security-Pipeline \
                        --exclude-paths=/var/jenkins_home/workspace/MoviesAPI-Security-Pipeline/trufflehog-exclude.txt \
                        --no-update \
                        > trufflehog-report.txt 2>&1 || true
        
                    cat trufflehog-report.txt
                '''
            }
        }

        stage('Build Image') {
            steps {
                sh '''
                    docker pull mcr.microsoft.com/dotnet/sdk:8.0
                    docker pull mcr.microsoft.com/dotnet/aspnet:8.0
        
                    docker build \
                        --pull \
                        --no-cache \
                        -t moviesapi-sec .
                '''
            }
        }

        stage('Trivy') {
            steps {
                sh '''
                docker run --rm \
                -v /var/run/docker.sock:/var/run/docker.sock \
                aquasec/trivy image \
                --scanners vuln \
                --pkg-types library \
                moviesapi-sec > trivy-report.txt 2>&1
        
                cat trivy-report.txt
                '''
            }
        }

        stage('Run Application') {
            steps {
                sh '''
                    docker compose down --remove-orphans || true
        
                    docker compose build \
                        --pull \
                        moviesapi
        
                    docker compose up \
                        -d \
                        --force-recreate \
                        moviesapi
        
                    echo "Waiting for MoviesAPI..."
        
                    for i in $(seq 1 30); do
                        if curl -fsS \
                            "http://moviesapi-security-pipeline-moviesapi-1:8080/swagger/v1/swagger.json" \
                            > /dev/null; then
        
                            echo "MoviesAPI is ready"
                            exit 0
                        fi
        
                        sleep 2
                    done
        
                    echo "MoviesAPI failed to start"
                    docker compose logs moviesapi
                    exit 1
                '''
            }
        }

        stage('SQLMap') {
            steps {
                sh '''
                    rm -rf sqlmap
                    git clone --depth 1 https://github.com/sqlmapproject/sqlmap.git
        
                    TARGETS_FILE="$WORKSPACE/Targets/endpoints-jenkins.txt"
        
                    if [ ! -s "$TARGETS_FILE" ]; then
                        echo "❌ Το αρχείο SQLMap targets λείπει ή είναι άδειο"
                        exit 1
                    fi
        
                    echo "===== SQLMap targets ====="
                    cat "$TARGETS_FILE"
                    echo "=========================="
        
                    set +e
        
                    script -q -e -c "python3 sqlmap/sqlmap.py \
                        -m '$TARGETS_FILE' \
                        -p title \
                        --level=5 \
                        --risk=3 \
                        --batch \
                        --flush-session \
                        --disable-coloring" /dev/null \
                        > sqlmap-report.txt 2>&1
        
                    SQLMAP_EXIT=$?
        
                    set -e
        
                    cat sqlmap-report.txt
                    echo "SQLMap exit code: $SQLMAP_EXIT"
        
                    exit 0
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
                        > zap-report.txt 2>&1
        
                    ZAP_EXIT=$?
        
                    set -e
        
                    cat zap-report.txt
        
                    echo "ZAP report files:"
                    ls -la zap-reports || true
        
                    if [ "$ZAP_EXIT" -eq 0 ]; then
                        echo "OWASP ZAP completed successfully"
                    elif [ "$ZAP_EXIT" -eq 2 ]; then
                        echo "OWASP ZAP completed with warnings"
                    elif [ "$ZAP_EXIT" -eq 1 ]; then
                        echo "OWASP ZAP detected blocking findings"
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
                    if [ ! -f zap-report.txt ]; then
                        echo "❌ ZAP console report missing"
                        FAILED=true
                    elif grep -Eqi "FAIL-NEW:[[:space:]]*[1-9][0-9]*|FAIL-INPROG:[[:space:]]*[1-9][0-9]*" zap-report.txt; then
                        echo "❌ OWASP ZAP detected blocking findings"
                        FAILED=true
                    else
                        echo "✅ OWASP ZAP completed without blocking failures"
                    fi
                    
                    if [ -f zap-reports/zap-report.html ]; then
                        echo "✅ ZAP HTML report generated"
                    else
                        echo "⚠️ ZAP HTML report was not generated"
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
                artifacts: 'semgrep-report.txt,trufflehog-report.txt,trivy-report.txt,sqlmap-report.txt,zap-report.html,zap-reports/*.html,zap-report.txt',
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