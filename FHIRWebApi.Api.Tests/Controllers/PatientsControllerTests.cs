
using FHIRHelpers.Api.Controllers;
using FHIRWebApi.Application.DTOs;
using FHIRWebApi.Services;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using Task = System.Threading.Tasks.Task;


namespace FHIRWebApi.Tests
{
    [TestClass]
    public class PatientsControllerTests
    {
        private Mock<IFhirPatientService> _mockFhirService;
        private Mock<IDistributedCache> _mockCache;
        private Mock<ILogger<PatientsController>> _mockLogger;
        private PatientsController _controller;

        [TestInitialize]
        public void Setup()
        {
            _mockFhirService = new Mock<IFhirPatientService>();
            _mockCache = new Mock<IDistributedCache>();
            _mockLogger = new Mock<ILogger<PatientsController>>();
            _controller = new PatientsController(_mockFhirService.Object, _mockCache.Object, _mockLogger.Object);
        }

        [TestMethod]
        public async Task GetPatients_ReturnsOk_WithData()
        {
            var bundle = new Bundle
            {
                Entry = new List<Bundle.EntryComponent>
                {
                    new Bundle.EntryComponent
                    {
                        Resource = new Patient { Id = "123" }
                    }
                }
            };

            _mockFhirService
                .Setup(s => s.SearchPatientsAsync(It.IsAny<SearchParams>()))
                .ReturnsAsync(bundle);

            var result = await _controller.GetPatients(20);

            Assert.IsInstanceOfType(result.Result, typeof(OkObjectResult));
        }

        [TestMethod]
        public async Task GetPatient_ReturnsPatient_WhenFound()
        {
            var patient = new Patient { Id = "abc" };
            _mockFhirService.Setup(s => s.ReadPatientAsync("abc")).ReturnsAsync(patient);

            var result = await _controller.GetPatient("abc");

            Assert.IsInstanceOfType(result.Result, typeof(OkObjectResult));
            var ok = result.Result as OkObjectResult;
            Assert.AreEqual("abc", ((Patient)ok!.Value!).Id);
        }

        [TestMethod]
        public async Task GetPatient_ReturnsNotFound_WhenMissing()
        {
            _mockFhirService
                .Setup(s => s.ReadPatientAsync(It.IsAny<string>()))
                .ThrowsAsync(new Hl7.Fhir.Rest.FhirOperationException("Not Found", System.Net.HttpStatusCode.NotFound));

            var result = await _controller.GetPatient("missing");

            Assert.IsInstanceOfType(result.Result, typeof(NotFoundObjectResult));
        }

        [TestMethod]
        public async Task CreatePatient_ReturnsCreated()
        {
            var request = new CreatePatientRequest
            {
                GivenName = "John",
                FamilyName = "Doe",
                Gender = "male",
                BirthDate = "1985-05-01"
            };

            var created = new Patient { Id = "p1" };
            _mockFhirService.Setup(s => s.CreatePatientAsync(It.IsAny<Patient>())).ReturnsAsync(created);

            var result = await _controller.CreatePatient(request);

            Assert.IsInstanceOfType(result.Result, typeof(CreatedAtActionResult));
        }

        [TestMethod]
        public async Task UpdatePatient_ReturnsOk()
        {
            var updated = new Patient { Id = "u1" };
            _mockFhirService.Setup(s => s.UpdatePatientAsync(It.IsAny<Patient>())).ReturnsAsync(updated);

            var result = await _controller.UpdatePatient("u1", updated);

            Assert.IsInstanceOfType(result.Result, typeof(OkObjectResult));
        }

        [TestMethod]
        public async Task DeletePatient_ReturnsNoContent_WhenSuccessful()
        {
            _mockFhirService.Setup(s => s.DeletePatientAsync("d1")).Returns(Task.CompletedTask);

            var result = await _controller.DeletePatient("d1");

            Assert.IsInstanceOfType(result, typeof(NoContentResult));
        }

        [TestMethod]
        public async Task DeletePatient_ReturnsNotFound_WhenMissing()
        {
            _mockFhirService
                .Setup(s => s.DeletePatientAsync(It.IsAny<string>()))
                .ThrowsAsync(new Hl7.Fhir.Rest.FhirOperationException("Not Found", System.Net.HttpStatusCode.NotFound));

            var result = await _controller.DeletePatient("missing");

            Assert.IsInstanceOfType(result, typeof(NotFoundObjectResult));
        }
    }
}
