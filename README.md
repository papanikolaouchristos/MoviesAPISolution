# MoviesAPI – Αυτοματοποιημένο DevSecOps Pipeline

## Περιγραφή

Το συγκεκριμένο repository περιλαμβάνει την εφαρμογή **MoviesAPI**, μια REST API εφαρμογή για διαχείρηση κινηματογραφικών αιθουσών και ταινιών αναπτυγμένη με **ASP.NET Core 8**, η οποία χρησιμοποιείται για την υλοποίηση ενός αυτοματοποιημένου DevSecOps pipeline.

Το pipeline εκτελεί στατικούς και δυναμικούς ελέγχους ασφάλειας κατά τη διαδικασία ανάπτυξης και ενεργοποιείται αυτόματα μετά από push στο GitHub repository.

Για την ενορχήστρωση του pipeline χρησιμοποιείται το **Jenkins**, ενώ η εφαρμογή και τα εργαλεία ασφάλειας εκτελούνται, όπου είναι δυνατό, μέσα σε Docker containers.

---

## Τεχνολογίες

Η εφαρμογή και το pipeline χρησιμοποιούν:

* ASP.NET Core 8
* Entity Framework Core
* SQLite
* Docker
* Docker Compose
* Jenkins
* Git και GitHub
* Git hooks
* GitHub Webhooks
* ngrok
* Semgrep
* TruffleHog
* Trivy
* sqlmap
* OWASP ZAP

---

## Αρχιτεκτονική pipeline

Η βασική ροή του pipeline είναι:

```text
Developer
    |
    | git commit / git push
    v
Git Hooks
    |
    | Local security checks
    v
GitHub Repository
    |
    | GitHub Webhook
    v
ngrok Tunnel
    |
    v
Jenkins
    |
    +--> Checkout
    +--> Semgrep
    +--> TruffleHog
    +--> Docker Build
    +--> Trivy
    +--> Run MoviesAPI
    +--> sqlmap
    +--> OWASP ZAP
    +--> Security Gate
    +--> Archive Reports
```

---

## Δομή repository

```text
MoviesAPI/
│
├── Controllers/
├── DTOs/
├── Data/
├── Migrations/
├── Models/
├── Properties/
│
├── Targets/
│   ├── endpoints.txt
│   └── endpoints-jenkins.txt
│
├── semgrep-rules/
│
├── .githooks/
│
├── jenkins/
│   └── Dockerfile
│
├── Dockerfile
├── docker-compose.yml
├── Jenkinsfile
├── MoviesAPI.csproj
├── Program.cs
├── appsettings.json
├── movies.db
└── README.md
```

Το αρχείο `Targets/endpoints.txt` χρησιμοποιείται από το GitHub Actions workflow, ενώ το `Targets/endpoints-jenkins.txt` περιλαμβάνει τα endpoints με τα ονόματα υπηρεσιών που χρησιμοποιούνται στο Docker δίκτυο του Jenkins.

---

## Προαπαιτούμενα

Για την εκτέλεση της εφαρμογής και του pipeline απαιτούνται:

* Git
* Docker Desktop
* Docker Compose
* GitHub account
* ngrok account
* Ενεργοποιημένο WSL 2 backend στο Docker Desktop για Windows

Έλεγχος εγκατάστασης:

```powershell
git --version
docker --version
docker compose version
ngrok version
```

---

## Εκκίνηση του περιβάλλοντος

Από PowerShell, μεταβείτε στον φάκελο του project:

```powershell
cd C:\Users\User\Desktop\MoviesAPISolution\MoviesAPI
```

Δημιουργήστε και ξεκινήστε τα containers:

```powershell
docker compose up -d --build
```

Ελέγξτε ότι τα containers εκτελούνται:

```powershell
docker ps
```

Θα πρέπει να εμφανίζονται τουλάχιστον:

```text
jenkins
moviesapi
```

ή ένα όνομα εφαρμογής που έχει παραχθεί από το Docker Compose, όπως:

```text
moviesapi-security-pipeline-moviesapi-1
```

---

## Πρόσβαση στις υπηρεσίες

### MoviesAPI

Η εφαρμογή είναι διαθέσιμη στη διεύθυνση:

```text
http://localhost:5000/swagger/index.html
```

### Jenkins

Το Jenkins είναι διαθέσιμο στη διεύθυνση:

```text
http://localhost:8081
```

---

## Αρχική ρύθμιση Jenkins

Κατά την πρώτη εκκίνηση, το Jenkins ζητά το αρχικό administrator password.

Η ανάκτησή του γίνεται με:

```powershell
docker exec jenkins cat /var/jenkins_home/secrets/initialAdminPassword
```

Το password εισάγεται στη σελίδα:

```text
http://localhost:8081
```

Στη συνέχεια:

1. Επιλέγεται η εγκατάσταση των προτεινόμενων plugins.
2. Δημιουργείται administrator χρήστης.
3. Εγκαθίστανται, αν δεν υπάρχουν ήδη, τα plugins:

   * Pipeline
   * Git
   * GitHub
   * GitHub Integration
   * Credentials Binding
   * Workspace Cleanup
   * HTML Publisher, προαιρετικά

---

## Δημιουργία Jenkins Pipeline

Στο Jenkins:

1. Επιλέξτε **New Item**.
2. Δώστε όνομα:

```text
MoviesAPI-Security-Pipeline
```

3. Επιλέξτε **Pipeline**.
4. Στην ενότητα Pipeline επιλέξτε:

```text
Pipeline script from SCM
```

5. SCM:

```text
Git
```

6. Repository URL:

```text
https://github.com/papanikolaouchristos/MoviesAPISolution.git
```

7. Branch:

```text
*/master
```

8. Script Path:

```text
Jenkinsfile
```

9. Στην ενότητα Build Triggers ενεργοποιήστε:

```text
GitHub hook trigger for GITScm polling
```

---

## Πρόσβαση Jenkins στο Docker

Το Jenkins container χρειάζεται πρόσβαση στο Docker daemon του host, ώστε να δημιουργεί και να εκτελεί containers.

Στο `docker-compose.yml` χρησιμοποιείται:

```yaml
volumes:
  - jenkins_home:/var/jenkins_home
  - /var/run/docker.sock:/var/run/docker.sock
```

Ο Jenkins container εκτελείται ως root:

```yaml
user: root
```

Έλεγχος πρόσβασης:

```powershell
docker exec jenkins docker --version
docker exec jenkins docker ps
```

Αν και οι δύο εντολές λειτουργούν, το Jenkins μπορεί να επικοινωνήσει με το Docker daemon.

---

## Ρύθμιση GitHub Webhook

Επειδή το Jenkins εκτελείται τοπικά, το GitHub δεν μπορεί να προσπελάσει απευθείας το `localhost`.

Χρησιμοποιείται το ngrok για τη δημιουργία δημόσιου HTTPS tunnel.

Εκκίνηση tunnel:

```powershell
ngrok http 8081
```

Το ngrok εμφανίζει μία δημόσια διεύθυνση, για παράδειγμα:

```text
https://example.ngrok-free.app
```

Στο GitHub repository μεταβείτε:

```text
Settings
→ Webhooks
→ Add webhook
```

Στο Payload URL εισάγετε:

```text
https://example.ngrok-free.app/github-webhook/
```

Ρυθμίσεις webhook:

```text
Content type: application/json
SSL verification: Enable
Events: Just the push event
Active: Enabled
```

Μετά από κάθε επανεκκίνηση του δωρεάν ngrok tunnel μπορεί να αλλάξει η δημόσια διεύθυνση. Σε αυτή την περίπτωση πρέπει να ενημερωθεί το Payload URL στο GitHub webhook.

Επιτυχής παράδοση webhook εμφανίζεται στο GitHub ως:

```text
Last delivery was successful
```

Στο Jenkins build εμφανίζεται:

```text
Started by GitHub push
```

---

## Git hooks

Τα Git hooks βρίσκονται στον φάκελο:

```text
.githooks/
```

Για την ενεργοποίησή τους εκτελέστε:

```powershell
git config core.hooksPath .githooks
```

Έλεγχος ρύθμισης:

```powershell
git config --get core.hooksPath
```

Αναμενόμενο αποτέλεσμα:

```text
.githooks
```

Τα hooks χρησιμοποιούνται για γρήγορους τοπικούς ελέγχους πριν την αποστολή του κώδικα.
στον φάκελο logs δημιουργούνται 2 αρχεία για Semgrep και TruffleHog
Οι πιο εκτεταμένοι και χρονοβόροι έλεγχοι εκτελούνται στο Jenkins μετά το push.
Αν θέλουμε να παρακάμψουμε τους τοπικούς ελέγχους μπορούμε μετά το commit να τρέξουμε από PowerShell στο path του project

```powershell
git push --no-verify
```

---

## Στάδια του Jenkins pipeline

### 1. Checkout

Το Jenkins κατεβάζει την τελευταία έκδοση του κώδικα από το GitHub repository.

### 2. Semgrep

Το Semgrep πραγματοποιεί Static Application Security Testing στον πηγαίο κώδικα C#.

Χρησιμοποιούνται custom rules από τον φάκελο:

```text
semgrep-rules/
```

Το report αποθηκεύεται ως:

```text
semgrep-report.txt
```

### 3. TruffleHog

Το TruffleHog ελέγχει το repository για:

* passwords,
* API tokens,
* private keys,
* credentials,
* secrets σε configuration files.

Το report αποθηκεύεται ως:

```text
trufflehog-report.txt
```

### 4. Build Docker image

Η εφαρμογή γίνεται build ως Docker image:

```text
moviesapi-sec
```

Το build χρησιμοποιεί ενημερωμένες εικόνες .NET 8 και αποφεύγει την επαναχρησιμοποίηση παλιών ευάλωτων cached layers.

### 5. Trivy

Το Trivy ελέγχει τις βιβλιοθήκες και τα dependencies που περιλαμβάνονται στο Docker image.

Το report αποθηκεύεται ως:

```text
trivy-report.txt
```

Ο έλεγχος του Security Gate εστιάζει στις βιβλιοθήκες της εφαρμογής και στο .NET runtime.

### 6. Run Application

Η εφαρμογή εκκινείται μέσω Docker Compose.

Το Jenkins περιμένει έως ότου απαντήσει επιτυχώς το endpoint:

```text
http://moviesapi:8080/swagger/v1/swagger.json
```

Έτσι οι δυναμικοί έλεγχοι δεν ξεκινούν πριν η εφαρμογή είναι έτοιμη.

### 7. sqlmap

Το sqlmap ελέγχει προκαθορισμένα endpoints για SQL injection.

Τα endpoints του Jenkins βρίσκονται στο:

```text
Targets/endpoints-jenkins.txt
```

Παράδειγμα:

```text
http://moviesapi:8080/api/movies/search?title=Batman
```

Το report αποθηκεύεται ως:

```text
sqlmap-report.txt
```

### 8. OWASP ZAP

Το OWASP ZAP εκτελεί baseline dynamic application security testing στη διεύθυνση:

```text
http://moviesapi:8080/swagger/index.html
```

Παράγει:

```text
zap-report.txt
zap-reports/zap-report.html
```

Τα warnings καταγράφονται χωρίς να αποτυγχάνει αυτόματα ολόκληρο το pipeline.

Τα πραγματικά blocking findings αξιολογούνται από το Security Gate.

### 9. Security Gate

Το Security Gate αξιολογεί τα αποτελέσματα όλων των εργαλείων.

Το pipeline αποτυγχάνει όταν:

* το Semgrep εντοπίσει blocking εύρημα,
* το TruffleHog εντοπίσει secret,
* το sqlmap εντοπίσει injectable parameter,
* το sqlmap δεν μπορεί να συνδεθεί στην εφαρμογή,
* το Trivy εντοπίσει blocking CRITICAL vulnerability,
* το OWASP ZAP εντοπίσει FAIL finding,
* λείπει απαραίτητο report.

Warnings του OWASP ZAP καταγράφονται, αλλά δεν αντιμετωπίζονται ως blocking failures.

### 10. Archive Artifacts

Το Jenkins αποθηκεύει τα reports ως build artifacts:

```text
semgrep-report.txt
trufflehog-report.txt
trivy-report.txt
sqlmap-report.txt
zap-report.txt
zap-reports/zap-report.html
```

Τα artifacts είναι διαθέσιμα μέσα από τη σελίδα κάθε Jenkins build.

---

## Χειροκίνητη εκτέλεση pipeline

Το pipeline εκτελείται αυτόματα μετά από push.

Μπορεί επίσης να εκτελεστεί χειροκίνητα:

1. Ανοίξτε το Jenkins.
2. Επιλέξτε:

```text
MoviesAPI-Security-Pipeline
```

3. Πατήστε:

```text
Build Now
```

4. Ανοίξτε:

```text
Console Output
```

για να δείτε τα αποτελέσματα.

---

## Αυτόματη εκτέλεση μέσω GitHub

Παράδειγμα αλλαγής:

```powershell
git add .
git commit -m "Update application"
git push
```

Η αναμενόμενη ροή είναι:

```text
Git push
→ GitHub webhook
→ ngrok
→ Jenkins
→ Security pipeline
```

Στο Jenkins Console Output εμφανίζεται:

```text
Started by GitHub push by papanikolaouchristos
```

---

## Αρχεία endpoints

### GitHub Actions

Το αρχείο:

```text
Targets/endpoints.txt
```

περιλαμβάνει endpoints που χρησιμοποιούν διευθύνσεις προσβάσιμες από το GitHub Actions ή από το αντίστοιχο workflow.

Παράδειγμα:

```text
http://localhost:8080/api/Movies/search?title=Batman
```

### Jenkins

Το αρχείο:

```text
Targets/endpoints-jenkins.txt
```

χρησιμοποιεί το όνομα της υπηρεσίας του Docker Compose:

```text
http://moviesapi:8080/api/movies/search?title=Batman
```

Μέσα σε Docker container το `localhost` αναφέρεται στο ίδιο το container και όχι στο MoviesAPI. Για αυτό στο Jenkins χρησιμοποιείται το service name `moviesapi`.

---

## Reports και αποτελέσματα

Μετά την ολοκλήρωση ενός Jenkins build:

1. Ανοίξτε το συγκεκριμένο build.
2. Επιλέξτε τα archived artifacts.
3. Κατεβάστε τα reports.

Τα βασικά reports είναι:

| Εργαλείο       | Report                        |
| -------------- | ----------------------------- |
| Semgrep        | `semgrep-report.txt`          |
| TruffleHog     | `trufflehog-report.txt`       |
| Trivy          | `trivy-report.txt`            |
| sqlmap         | `sqlmap-report.txt`           |
| OWASP ZAP      | `zap-report.txt`              |
| OWASP ZAP HTML | `zap-reports/zap-report.html` |

---

## Ενδεικτικά αποτελέσματα

Σε επιτυχημένη εκτέλεση αναμένονται αποτελέσματα όπως:

```text
Semgrep:
Targets scanned: 50
Findings: 0
```

```text
TruffleHog:
verified_secrets: 0
unverified_secrets: 0
```

```text
sqlmap:
GET parameter 'title' does not seem to be injectable
```

```text
OWASP ZAP:
FAIL-NEW: 0
WARN-NEW: 9
```

```text
SECURITY GATE PASSED
Finished: SUCCESS
```

---

## Σκόπιμα εισαγμένες ευπάθειες

Για την αξιολόγηση του pipeline χρησιμοποιήθηκαν ελεγχόμενες ευπάθειες, όπως:

* μη ασφαλής δημιουργία SQL query,
* χρήση `FromSqlRaw` με μη ασφαλή συμβολοσειρά,
* hardcoded token ή credential σε configuration file,
* παλιό .NET runtime Docker image,
* ελλιπή HTTP security headers,
* πιθανώς ευάλωτα dependencies.

Οι ευπάθειες χρησιμοποιήθηκαν αποκλειστικά σε τοπικό και απομονωμένο περιβάλλον για εκπαιδευτικούς σκοπούς.

Μετά τον εντοπισμό τους εφαρμόστηκαν διορθώσεις και το pipeline εκτελέστηκε ξανά για την επιβεβαίωση των αποτελεσμάτων.

---

## Βασικές διορθώσεις ασφάλειας

Οι βασικές διορθώσεις περιλαμβάνουν:

* αντικατάσταση raw SQL queries με parameterized queries,
* αφαίρεση hardcoded secrets,
* αναβάθμιση Docker base images,
* αναβάθμιση .NET runtime,
* έλεγχο NuGet dependencies,
* προσθήκη HTTP security headers,
* περιορισμό των Docker permissions,
* καταγραφή και αξιολόγηση των ZAP warnings.

---

## Αντιμετώπιση προβλημάτων

### Το Jenkins δεν μπορεί να χρησιμοποιήσει Docker

Έλεγχος:

```powershell
docker exec jenkins docker ps
```

Αν εμφανίζεται permission error, ελέγξτε ότι:

```yaml
user: root
```

και:

```yaml
- /var/run/docker.sock:/var/run/docker.sock
```

υπάρχουν στο `docker-compose.yml`.

### Το GitHub webhook παραδίδεται αλλά δεν ξεκινά build

Ελέγξτε ότι στο Jenkins job είναι ενεργοποιημένο:

```text
GitHub hook trigger for GITScm polling
```

και ότι το Payload URL καταλήγει σε:

```text
/github-webhook/
```

### Το sqlmap εμφανίζει Connection refused

Το αρχείο Jenkins endpoints πρέπει να χρησιμοποιεί:

```text
http://moviesapi:8080
```

και όχι:

```text
http://localhost:8080
```

### Το ZAP επιστρέφει exit code 2

Το exit code `2` στο baseline scan σημαίνει ότι εντοπίστηκαν warnings χωρίς blocking failures.

Το pipeline χειρίζεται το αποτέλεσμα ως προειδοποίηση.

### Το Trivy εμφανίζει παλιό .NET runtime

Εκτελέστε νέο pull και build χωρίς cache:

```powershell
docker pull mcr.microsoft.com/dotnet/sdk:8.0
docker pull mcr.microsoft.com/dotnet/aspnet:8.0
docker build --pull --no-cache -t moviesapi-sec .
```

---

## Τερματισμός περιβάλλοντος

Για να σταματήσετε τα services:

```powershell
docker compose down
```

Για διαγραφή και των volumes:

```powershell
docker compose down -v
```

Προσοχή: η επιλογή `-v` διαγράφει και το Jenkins volume, μαζί με τις ρυθμίσεις και τα αποθηκευμένα Jenkins jobs.

---

## Περιορισμοί

Η παρούσα υλοποίηση έχει τους ακόλουθους περιορισμούς:

* Το Jenkins εκτελείται τοπικά.
* Το ngrok URL μπορεί να αλλάζει μετά από επανεκκίνηση.
* Το OWASP ZAP baseline scan πραγματοποιεί κυρίως passive scanning.
* Το sqlmap ελέγχει μόνο τα προκαθορισμένα endpoints.
* Δεν εκτελούνται authenticated DAST tests.
* Το Security Gate βασίζεται εν μέρει στην ανάλυση των παραγόμενων reports.
* Ορισμένα vulnerabilities μπορεί να προέρχονται από base images ή transitive dependencies.
* Τα αποτελέσματα εξαρτώνται από την ενημέρωση των vulnerability databases.

---

## Ασφάλεια και εξουσιοδότηση

Τα εργαλεία sqlmap και OWASP ZAP πρέπει να χρησιμοποιούνται μόνο σε εφαρμογές και περιβάλλοντα για τα οποία υπάρχει ρητή άδεια ελέγχου.

Στο συγκεκριμένο project οι δυναμικοί έλεγχοι εκτελούνται αποκλειστικά στην τοπική MoviesAPI εφαρμογή, σε Docker περιβάλλον που ελέγχεται πλήρως από τον δημιουργό του project.

---

## Δημιουργός

**Χρήστος Παπανικολάου**

Μεταπτυχιακό Πρόγραμμα Σπουδών
Πανεπιστήμιο Πειραιώς

---

## Άδεια χρήσης

Το project δημιουργήθηκε για εκπαιδευτικούς σκοπούς στο πλαίσιο εργασίας DevSecOps.

Δεν επιτρέπεται η χρήση των επιθετικών εργαλείων του project σε συστήματα τρίτων χωρίς προηγούμενη ρητή εξουσιοδότηση.
