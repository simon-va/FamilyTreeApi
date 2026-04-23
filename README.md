# FamilyTreeApiV2

## Lokale Datenbank

Die lokale PostgreSQL-Datenbank wird via Docker Compose betrieben.

### Voraussetzungen

- Docker Desktop

### Datenbank starten

```bash
docker compose up -d
```

### Datenbank zurücksetzen und neu aufsetzen

```bash
docker compose down -v
docker compose up -d
```

Das `-v` löscht das Volume, sodass `db/init.sql` beim nächsten Start neu ausgeführt wird.

### Seed-Daten einspielen

**macOS / Linux:**
```bash
docker compose exec -T db psql -U postgres -d familytree < db/seed.sql
```

**Windows (PowerShell):**
```powershell
Get-Content db/seed.sql | docker compose exec -T db psql -U postgres -d familytree
```

### Connection String

Der Connection String für die lokale Datenbank ist in `appsettings.Development.json` hinterlegt und greift automatisch, wenn die API mit `ASPNETCORE_ENVIRONMENT=Development` gestartet wird.

> **Hinweis:** User Secrets überschreiben den Connection String aus `appsettings.Development.json`.
