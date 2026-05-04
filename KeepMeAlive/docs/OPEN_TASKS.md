# KeepMeAlive: Offene Arbeiten

Stand: 30.04.2026

Diese Datei sammelt die Punkte, die nach dem aktuellen Umbau noch offen sind, damit `KeepMeAlive` production-ready wird.

## 1. Branding und Repository

- [ ] GitHub-Repository von `StatusUpdater` auf `KeepMeAlive` umbenennen, falls noch nicht passiert.
- [ ] Release-Assets auf den neuen Namen umstellen:
  - `KeepMeAlive-Setup.exe`
  - `KeepMeAlive-Portable.zip`
- [ ] Installer-Skripte, CI/CD und Release-Workflows auf neue Pfade/Namen prüfen.
- [ ] Optional: Workspace-/Repo-Ordner auch physisch auf `KeepMeAlive` umziehen.

## 2. Account-Seite fertig machen

- [ ] `AccountService`-Stub durch echte Datenquelle ersetzen.
- [ ] Login-/Logout-Flow für Benutzer einbauen.
- [ ] Benutzerprofil laden:
  - Display Name
  - E-Mail
  - Workspace / Tenant
  - Auth Provider
- [ ] Lade-, Fehler- und Offline-Zustände sauber in der UI darstellen.
- [ ] "Refresh" durch automatische Synchronisierung beim App-Start und nach Login ergänzen.
- [ ] Optional: Avatar/Initialen und letzte erfolgreiche Anmeldung anzeigen.

## 3. Supabase-Integration

- [ ] Supabase-Projekt anlegen.
- [ ] Auth-Strategie festlegen:
  - E-Mail + Magic Link
  - E-Mail + Passwort
  - OAuth, z. B. Google oder Microsoft
- [ ] Tabellenmodell definieren:
  - `profiles`
  - `subscriptions` oder `licenses`
  - `entitlements`
  - optional `devices`
- [ ] Row Level Security Policies definieren.
- [ ] Session-Handling in der Desktop-App implementieren.
- [ ] Secure Token Storage für Windows einbauen.
  - Ziel: keine Tokens im Klartext in `settings.json`
- [ ] AccountViewModel an Supabase Auth + Profil-Daten anbinden.
- [ ] Optional: Supabase Edge Functions für Lizenz-/Billing-Abgleiche einsetzen.

## 4. Billing und Licensing

- [ ] Entscheiden, ob Billing über Paddle oder einen anderen Anbieter laufen soll.
- [ ] Lizenzmodell final definieren:
  - Trial
  - aktives Abo
  - abgelaufen
  - Grace Period
- [ ] Backend-Quelle für Lizenzstatus definieren.
- [ ] Lokales Speichern des License Keys durch echte Validierung ersetzen.
- [ ] Webhooks für Billing-Events verarbeiten:
  - Kauf
  - Verlängerung
  - Kündigung
  - Payment Failure
- [ ] Entitlements in der App auswerten, statt nur den Key zu speichern.
- [ ] UI-Texte auf echte Plans, Vorteile und Ablaufdaten umstellen.

## 5. Security und Datenschutz

- [ ] Sensitive Daten aus `settings.json` herausziehen oder verschlüsseln.
- [ ] Tokens und Secrets im Windows Credential Manager oder vergleichbar sicher speichern.
- [ ] Logging so bauen, dass keine persönlichen Daten oder Schlüssel versehentlich geloggt werden.
- [ ] Privacy Policy und Terms of Service verlinken und mit echten Zielseiten hinterlegen.
- [ ] Opt-in für Telemetrie/Produktanalytik definieren, falls Analytics eingesetzt werden.
- [ ] Threat Model für Desktop-App + Auth + Billing einmal explizit dokumentieren.

## 6. Observability und Support

- [ ] Crash Reporting einbauen, z. B. mit Sentry.
- [ ] Strukturierte Logs einführen.
- [ ] Wichtige technische Events tracken:
  - App gestartet
  - Keep-alive gestartet/gestoppt
  - Auth erfolgreich/fehlgeschlagen
  - Lizenz validiert/ungültig
- [ ] Fehlerzustände mit User-freundlichen Meldungen versehen.
- [ ] Optional: In-App "Support / Feedback senden" ergänzen.

## 7. Analytics und Feature Flags

- [ ] Produktanalytik einbauen, z. B. mit PostHog.
- [ ] Kern-Events definieren:
  - App installiert
  - erster erfolgreicher Start
  - Keep-alive aktiviert
  - Trial gestartet
  - Upgrade geklickt
  - Upgrade abgeschlossen
- [ ] Funnel definieren:
  - Install
  - App geöffnet
  - Keep-alive gestartet
  - Account erstellt
  - Lizenz aktiviert / Upgrade
- [ ] Feature Flags für riskante oder experimentelle Features vorbereiten.

## 8. UX- und Conversion-Optimierung

### Sofort sinnvoll

- [ ] Dashboard noch stärker auf "1-Klick-Start" optimieren.
- [ ] Standard-Preset setzen, damit neue Nutzer keine Methode auswählen müssen.
- [ ] Hero-Status noch klarer formulieren:
  - Was passiert beim Klick?
  - Ist die App unauffällig?
  - Braucht sie Admin-Rechte?
- [ ] Account-Seite um klaren monetären CTA ergänzen:
  - Upgrade
  - Trial starten
  - Plan vergleichen

### Friktion reduzieren

- [ ] Freitext-Feld für `Date & time` ersetzen.
  - Ziel: DatePicker + TimePicker
- [ ] Expertenfelder verstecken oder hinter "Expert Mode" legen:
  - Virtual key code
  - Mouse pixel delta
- [ ] Copy an mehreren Stellen weniger technisch formulieren.
- [ ] Empty States für nicht eingeloggte User verbessern.

### Später testen

- [ ] Verschiedene CTA-Texte testen:
  - `Start Keep-Alive`
  - `Stay Available`
  - `Keep Me Active`
- [ ] Unterschiedliche Upgrade-Platzierungen testen.
- [ ] Trial-Hinweise gegen "Feature locked" vergleichen.
- [ ] Optional: kleines Onboarding beim ersten Start ergänzen.

## 9. Desktop-spezifische Release-Themen

- [ ] Code Signing für Windows-Distribution einrichten.
- [ ] SmartScreen-Reputation für Download-Distribution einplanen.
- [ ] Installer-Härtung prüfen:
  - saubere Uninstall-Routine
  - Auto-Start korrekt entfernen
  - Settings behalten oder optional löschen
- [ ] Update-Strategie finalisieren:
  - GitHub Releases weiter nutzen oder wechseln
  - Delta/Background Updates ja oder nein
- [ ] Fallback-Verhalten definieren, wenn GitHub oder Update-Endpunkt nicht erreichbar ist.

## 10. Tests und Qualität

- [ ] Unit Tests für ViewModels ergänzen.
- [ ] Tests für Settings- und Account-Flows ergänzen.
- [ ] Lizenz-/Subscription-Status als klar testbare Zustandsmaschine modellieren.
- [ ] Fehlerfälle testen:
  - kein Internet
  - ungültige Session
  - abgelaufene Lizenz
  - Billing-Webhooks verzögert
- [ ] Explorative UI-Tests auf kleinen Displays durchführen.
- [ ] Tray-, Startup- und Update-Verhalten manuell auf frischem Windows-Testsystem prüfen.

## 11. Dokumentation

- [ ] README final an Produktstatus anpassen, sobald Auth/Billing live sind.
- [ ] Setup-Dokument für Supabase ergänzen:
  - benötigte Tabellen
  - RLS
  - Redirect URLs
  - lokale Entwicklung
- [ ] Billing-/Webhook-Dokumentation ergänzen.
- [ ] Release-Prozess dokumentieren:
  - Build
  - Signieren
  - Package
  - Publish

## 12. Empfohlene externe Dienste

### Kern

- [ ] Supabase für Auth, Datenmodell und Session-Verwaltung
- [ ] Billing-Anbieter, bevorzugt Paddle, für Subscriptions und Steuer-/Compliance-Themen
- [ ] Code Signing für Windows-Installer und Binaries

### Stark empfohlen

- [ ] Sentry für Crash Reporting und Performance Monitoring
- [ ] PostHog für Product Analytics und Feature Flags

### Optional

- [ ] Resend oder vergleichbarer Mail-Dienst für transaktionale E-Mails
- [ ] Helpdesk/Support-System, falls In-App Support geplant ist

## 13. Empfohlene Reihenfolge

### Phase 1: Funktionsfähig machen

- [ ] Supabase Auth anbinden
- [ ] sichere Session-/Token-Speicherung bauen
- [ ] Account-Seite mit echten Daten füllen

### Phase 2: Monetarisierung

- [ ] Billing-Anbieter anbinden
- [ ] Lizenzstatus serverseitig verwalten
- [ ] Upgrade-/Trial-Flows in der App sichtbar machen

### Phase 3: Betrieb

- [ ] Crash Reporting
- [ ] Analytics
- [ ] Code Signing
- [ ] Release-Pipeline

### Phase 4: Conversion

- [ ] 1-Klick-Start weiter vereinfachen
- [ ] Scheduling-UX modernisieren
- [ ] Expertenoptionen stärker verstecken
- [ ] CTA- und Upgrade-Platzierungen testen

