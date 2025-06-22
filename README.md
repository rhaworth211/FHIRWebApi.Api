# FHIRWebApi.Api

**FHIRWebApi.Api** is a C#/.NET-based Web API designed to interact with FHIR-compliant healthcare systems. It enables developers to query, create, and manage FHIR resources such as Patients and Observations using standardized formats and validation.

---

## 🌐 Features

- 🌱 Create and fetch **FHIR Patient** and **Observation** resources
- ✅ Validates requests using `System.ComponentModel.DataAnnotations`
- 🔧 Uses `Hl7.Fhir.R4` SDK for FHIR model operations
- ⚙️ Configurable FHIR server endpoints (e.g. Firely, HAPI)
- 🔍 Supports filtering and structured FHIR `SearchParams`
- 🔄 JSON format and version negotiation with `FhirClientSettings`

---

## 🚀 Getting Started

### Prerequisites

- [.NET 6+ SDK](https://dotnet.microsoft.com/download)
- (Optional) A running FHIR server like:
  - https://hapi.fhir.org/baseR4
  - https://server.fire.ly/

### Setup

```bash
git clone https://github.com/rhaworth211/FHIRWebApi.Api.git
cd FHIRWebApi.Api
dotnet restore
dotnet run
```

The API should now be running at `https://localhost:7091`.

---

## 📁 Project Structure

```bash
├── Controllers/
│   └── PatientsController.cs
│   └── ObservationsController.cs
├── DTOs/
│   └── CreateObservationRequest.cs
├── Program.cs
├── appsettings.json
```

---

## 🔌 Example API Usage

### GET: Retrieve Patients
```http
GET /api/patients
```

### POST: Create Observation
```http
POST /api/observations
Content-Type: application/json

{
  "SubjectId": "Patient/123",
  "CodeSystem": "http://loinc.org",
  "Code": "85354-9",
  "CodeDisplay": "Blood pressure panel",
  "Value": "120/80",
  "Unit": "mmHg"
}
```

---

## 🏥 FHIR Client Settings Example

```csharp
builder.Services.AddSingleton(new FhirClient("https://hapi.fhir.org/baseR4", new FhirClientSettings
{
    PreferredFormat = ResourceFormat.Json,
    VerifyFhirVersion = true
}));
```

---

## 📄 License

MIT License — see `LICENSE` for full text.

---

> Built by [Ryan Haworth](mailto:r.haworth@outlook.com)
