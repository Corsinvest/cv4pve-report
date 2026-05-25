# Compliance section

cv4pve-report can perform a **technical compliance assessment** of the Proxmox VE cluster against widely-adopted security and regulatory frameworks. When at least one standard is enabled, the report adds a dedicated **Compliance** section with an overview of every standard, per-control status, and a row per check (PASS / FAIL / N/A) with severity, scope and remediation hint.

The assessment is **automated and technical only** — it reads the cluster state and flags findings against the verifiable subset of each standard. Procedural, organisational and physical controls are out of scope and require manual review. The report **does not constitute formal certification**; use it as a continuous self-assessment input alongside your audit programme.

## Supported standards

| ID | Title | Mapping focus |
|---|---|---|
| `ISO27001` | ISO/IEC 27001:2022 | Annex A technical controls |
| `NIS2` | NIS2 — Directive (EU) 2022/2555 | Art. 21(2) minimum measures |
| `CIS` | CIS Controls v8 | Foundational & organisational safeguards |
| `AgID` | AgID Misure Minime ICT | Italian PA baseline (ABSC) |
| `PCIDSS` | PCI DSS v4.0 | Network / access / log requirements |
| `GDPR` | GDPR — Art. 32 Security of processing | Technical & organisational measures |
| `DORA` | DORA — Regulation (EU) 2022/2554 | ICT risk management for the financial sector |
| `NISTCSF` | NIST Cybersecurity Framework 2.0 | Govern / Identify / Protect / Detect / Respond / Recover |
| `ISO27017` | ISO/IEC 27017:2015 — Cloud Security Extensions | CLD.* cloud-specific extensions to ISO 27001 |

## Enabling standards

The compliance section is **off by default**. Toggle the standards you need in your `settings.json`:

```json
{
  "Compliance": {
    "ISO27001": true,
    "NIS2": true,
    "CIS": false,
    "AgID": false,
    "PCIDSS": false,
    "GDPR": false,
    "DORA": false,
    "NISTCSF": false,
    "ISO27017": false
  }
}
```

The `--full` profile enables **all** standards (intended for audit-time runs).

## How the assessment works

Every standard is decomposed into **controls** (`A.5.17`, `Art.21(j)`, `CIS-6`, …). Each control runs a list of **checks** against the cluster state. A check looks at a specific aspect (e.g. *"admin users without 2FA"*) and produces zero or more **findings** when something is non-conformant.

```
ICompliancePack          (e.g. ISO/IEC 27001:2022)
  └─ IComplianceControl  (e.g. A.5.17 — Authentication information)
       └─ IComplianceCheck × N
             └─ ComplianceFinding × M  (one per issue found)
```

The same check can be referenced by multiple standards — for example *admin without 2FA* is reused by ISO 27001 (A.5.17 / A.8.5), NIS2 Art.21(j), PCI DSS Req 8, NIST CSF PR.AA, and so on. This is intentional: each auditor sees the evidence under their own control.

### Status per control

For every control the report computes an aggregated status:

| Status | Meaning |
|---|---|
| ✓ **PASS** | All checks executed, no findings |
| ✗ **FAIL** | At least one finding |
| ◐ **PARTIAL** | Some checks executed and passed, others skipped because their input data was unavailable |
| — **N/A** | Every check was skipped (input data unavailable for all of them) |

### Score

Each pack carries an overall **score** (0–100 %): the average control score across non-N/A controls. A control with no findings on all its executable checks scores 100 %; a control where every check failed scores 0 %.

The score reflects the **automated subset** the report can verify — it is not a percentage of the standard's full compliance posture.

## Output

The compliance section is rendered consistently in every format:

| Format | Layout |
|---|---|
| **HTML** | Dedicated `Compliance` sidebar group with an **Overview** page plus one page per enabled pack under `compliance/<pack>.html` |
| **Excel** | Dedicated `Compliance` worksheet (overview) plus one `Compliance <PackId>` worksheet per enabled pack |
| **JSON** | `compliance.json` (overview) plus `compliance/<pack>.json` (detail) |

Each pack page contains:

- **Info** — pack id, title, controls/findings totals, overall score
- **Disclaimer** — scope and limits of the automated assessment
- **Controls** — one row per control: status (✓/✗/◐/—), score, checks, findings, skipped, highest severity
- **Checks** — one row per *check outcome* (PASS / FAIL / N/A); when a check produces multiple findings, each finding gets its own row with severity, scope, title, details and remediation hint

All tables are filterable — in HTML and Excel the standard column headers let you isolate `FAIL` rows or focus on `High`/`Critical` severities natively.

## Limits

- The assessment is **technical**. Organisational requirements (policies, training, supplier management, BCP testing) are out of scope.
- Coverage varies by standard: ISO 27001 maps ~25 of 93 Annex A controls; NIS2 covers ~5 of 10 Art.21(2) measures; PCI DSS and HIPAA-like requirements that involve cardholder/PHI data are not enforceable from PVE state alone.
- A 100 % score does **not** mean the cluster is fully compliant — it means every check the report can run passed.
