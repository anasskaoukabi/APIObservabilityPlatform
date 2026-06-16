# Plateforme de Supervision et d'Observabilité API (.NET)

Ce projet de fin d'études a pour objectif la conception et le développement d’une plateforme complète de supervision et d’observabilité dédiée au monitoring des API dans un environnement .NET.

La solution intègre la surveillance automatique de la santé d'une API CRUD témoin, la persistance des données historiques sous SQLite via EF Core, l'affichage de tableaux de bord interactifs en temps réel avec Blazor, et un système d'alerte par email utilisant MailKit.

---

## 🛠 Technologies Utilisées

*   **Framework principal** : .NET 9.0 SDK
*   **Interface utilisateur (Axe 4)** : Blazor Server (InteractiveServer Render Mode), CSS Vanilla personnalisé
*   **Vérification de la santé (Axe 2)** : Worker Service (.NET BackgroundService) utilisant `PeriodicTimer`
*   **Base de données (Axe 3)** : SQLite avec Entity Framework Core (approche *EnsureCreated* automatique au démarrage)
*   **Notifications par email** : MailKit & MimeKit (protocole SMTP sécurisé, alerte envoyée une seule fois lors de la panne pour éviter le spam)
*   **Visualisation** : SVG Sparklines dynamiques (latence) et Chart.js (historique de latence détaillé)

---

## 📂 Structure de la Solution

*   `1-Apps/`
    *   `TargetAPI/` : L'API CRUD témoin (gestion de produits) servant de cible à surveiller (port `http://localhost:5100`).
    *   `MonitoringWorker/` : Le Worker Service qui exécute périodiquement (toutes les 10s) les requêtes HTTP, mesure la latence, applique les seuils SLA, persiste les logs en base de données et envoie les emails.
    *   `Dashboard.Web/` : L'application web Blazor qui sert d'interface de supervision (port `http://localhost:5200`).
*   `2-Shared/`
    *   `Observability.Contracts/` : Projet contenant les entités partagées (`HealthCheckLog`, `MetricLog`), les DbContexts, et les contrats.
*   `monitoring.db` : Fichier de base de données SQLite généré automatiquement à la racine de la solution.

---

## 🚀 Installation & Démarrage

### 1. Prérequis
*   Avoir installé le **SDK .NET 9** (ou compatible).
*   Pour tester les alertes email localement, vous pouvez utiliser un outil comme **Mailpit** ou **Maildev** (qui tourne sur le port SMTP par défaut `1025`).

### 2. Configuration
Créez un fichier `appsettings.json` dans le dossier `1-Apps/MonitoringWorker/` en vous basant sur le fichier modèle `appsettings.Example.json` présent à la racine :
*   Configurez l'URL de l'API à surveiller.
*   Indiquez vos paramètres de connexion SMTP (si vous utilisez Gmail, configurez un mot de passe d'application).

### 3. Démarrage des Services
Ouvrez votre terminal à la racine du projet et démarrez chaque projet :

```bash
# 1. Démarrer l'API cible (port 5100)
dotnet run --project 1-Apps/TargetAPI

# 2. Démarrer le Worker de Health Check
dotnet run --project 1-Apps/MonitoringWorker

# 3. Démarrer le Dashboard Web Blazor (port 5200)
dotnet run --project 1-Apps/Dashboard.Web
```

Accédez ensuite au Dashboard à l'adresse suivante : `http://localhost:5200`
*(Identifiants administrateur par défaut : Nom d'utilisateur : `admin` | Mot de passe : `admin123`)*

---

## 🚦 Guide d'Utilisation & Simulation de Panne

Pour tester et présenter le comportement dynamique de la plateforme à votre encadrante, suivez ce protocole :

### Cas 1 : API Cible active (Nominal)
*   Démarrer les 3 projets.
*   Sur le Dashboard, le statut de l'API doit afficher **EN LIGNE** en vert.
*   La courbe de latence et les logs SQLite affichent des temps de réponse normaux (ex: `~5-10 ms`).

### Cas 2 : Coupure de l'API (Détection de panne & email)
*   **Action** : Arrêtez l'application `TargetAPI` dans votre terminal (via `Ctrl+C`).
*   **Observation Worker** : Au bout de 10 secondes maximum, le worker affiche en rouge :
    ```text
    [CRITICAL] API HORS SERVICE ou ERREUR détectée !
    ```
*   **Notification** : Le worker envoie **un seul email** d'alerte critique sur votre boîte de réception/simulateur SMTP. Aucun autre e-mail n'est envoyé lors des cycles suivants pour éviter de polluer votre boîte.
*   **Observation Dashboard** : La page d'accueil affiche le statut **HORS SERVICE** en rouge et le log de panne s'affiche en temps réel dans le tableau historique.

### Cas 3 : Restauration de l'API
*   **Action** : Redémarrez `TargetAPI` via `dotnet run --project 1-Apps/TargetAPI`.
*   **Observation Worker** : Au prochain tick (10 secondes), le worker détecte le retour à la normale et affiche en vert :
    ```text
    [SUCCESS] API est fonctionnelle (200 OK)
    ```
*   **Observation Dashboard** : Le statut repasse instantanément à **EN LIGNE** en vert.

### Cas 4 : Export de l'Historique
*   Sur la page d'accueil du Dashboard, cliquez sur le bouton **Exporter Historique en CSV 📥**.
*   Le téléchargement d'un fichier `.csv` contenant l'historique complet de toutes les mesures de santé s'exécute automatiquement.
