# FHIRWebApi.Api

**FHIRWebApi.Api** is a secure, modular .NET Web API for interacting with HL7® FHIR® resources such as `Patient` and `Observation`. It integrates Azure Redis caching, Azure Container Apps deployment, JWT authentication, and Firely or HAPI test FHIR servers.

---

## 🔧 Features

- 🔐 **JWT Authentication** with role-based authorization
- ⚕️ **FHIR Patient & Observation Controllers**
- 🚀 **Deployable via GitHub Actions** to Azure Container Apps with ACR
- 🧪 **MSTest-ready** with a clean testing structure
- 💾 **Azure Redis Caching** for performance
- 🩺 Supports **Create / Read / Update / Delete** (CRUD) operations

---

## 🗂 Project Structure

```
FHIRWebApi.Api/
├── Controllers/
│   ├── AuthController.cs
│   ├── PatientsController.cs
│   └── ObservationsController.cs
├── Application/
│   └── DTOs/
│       ├── CreatePatientRequest.cs
│       └── CreateObservationRequest.cs
├── Services/
│   └── FHIRPatientService.cs
├── Program.cs
└── deploy-containerapp-acr.yml
```

---

## 🛠 Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli)
- Azure Container Registry (ACR) + Azure Container App environment
- Optional: Azure Key Vault for Redis secrets

---

## 🚀 Running Locally

```bash
dotnet restore
dotnet build
dotnet run
```

API will run at: `https://localhost:5001` (or as configured in `launchSettings.json`)

---

## 🔐 Authentication

Login endpoint:  
```
POST /api/auth/login
{
  "username": "FhirDev",
  "password": "@ppl3314"
}
```

Returns:
```json
{
  "token": "JWT_TOKEN_HERE",
  "expires": "..."
}
```

Use the token in the `Authorization` header for all protected endpoints.

---

## 🧪 Running Tests

```bash
dotnet test
```

All unit tests reside in a separate MSTest project (not shown here).

---

## ☁️ Deploying to Azure

Deployment is managed via GitHub Actions:
- Builds the project
- Runs tests
- Builds/pushes image to ACR
- Updates Azure Container App

See `deploy-containerapp-acr.yml` for full workflow.

---

## 📚 References

- [FHIR Specification (R4)](https://www.hl7.org/fhir/)
- [Firely Test Server](https://server.fire.ly/)
- [Azure Redis](https://learn.microsoft.com/en-us/azure/azure-cache-for-redis/)
- [Azure Container Apps](https://learn.microsoft.com/en-us/azure/container-apps/)

---

## 👨‍💻 Author

**Ryan Haworth**  
📫 [r.haworth@outlook.com](mailto:r.haworth@outlook.com)  
🔗 [LinkedIn](https://www.linkedin.com/in/ryan-haworth)

---

## 📄 License

MIT License — see [`LICENSE`](LICENSE) for details.