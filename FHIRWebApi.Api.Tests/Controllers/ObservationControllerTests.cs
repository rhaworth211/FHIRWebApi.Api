
using FHIRWebApi.Application.DTOs;
using FHIRWebApi.Controllers;
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
    public class ObservationsControllerTests
    {
        private Mock<IFhirObservationService> _mockFhirService;
        private Mock<IDistributedCache> _mockCache;
        private Mock<ILogger<ObservationsController>> _mockLogger;
        private ObservationsController _controller;

        [TestInitialize]
        public void Setup()
        {
            _mockFhirService = new Mock<IFhirObservationService>();
            _mockCache = new Mock<IDistributedCache>();
            _mockLogger = new Mock<ILogger<ObservationsController>>();
            _controller = new ObservationsController(_mockFhirService.Object, _mockCache.Object, _mockLogger.Object);
        }

        [TestMethod]
        public async Task GetObservations_ReturnsAll_WhenNoPatientId()
        {
            var bundle = new Bundle
            {
                Entry = new List<Bundle.EntryComponent>
                {
                    new Bundle.EntryComponent
                    {
                        Resource = new Observation { Id = "obs1" }
                    }
                }
            };

            _mockFhirService
                .Setup(s => s.SearchObservationsAsync(It.IsAny<SearchParams>()))
                .ReturnsAsync(bundle);

            var result = await _controller.GetObservations(null);

            Assert.IsInstanceOfType(result.Result, typeof(OkObjectResult));
        }

        [TestMethod]
        public async Task GetObservation_ReturnsFound()
        {
            var obs = new Observation { Id = "obs123" };
            _mockFhirService.Setup(s => s.ReadObservationAsync("obs123")).ReturnsAsync(obs);

            var result = await _controller.GetObservation("obs123");

            Assert.IsInstanceOfType(result.Result, typeof(OkObjectResult));
        }

        [TestMethod]
        public async Task GetObservation_ReturnsNotFound()
        {
            _mockFhirService
                .Setup(s => s.ReadObservationAsync(It.IsAny<string>()))
                .ThrowsAsync(new Hl7.Fhir.Rest.FhirOperationException("Not found", System.Net.HttpStatusCode.NotFound));

            var result = await _controller.GetObservation("missing");

            Assert.IsInstanceOfType(result.Result, typeof(NotFoundResult));
        }

        [TestMethod]
        public async Task CreateObservation_ReturnsCreated()
        {
            var request = new CreateObservationRequest
            {
                SubjectId = "Patient/1",
                CodeSystem = "http://loinc.org",
                Code = "1234-5",
                CodeDisplay = "Example Code",
                Value = "100",
                Unit = "mg",
                UnitSystem = "http://unitsofmeasure.org",
                UnitCode = "mg",
                EffectiveDate = "2024-01-01"
            };

            var created = new Observation { Id = "newobs" };

            _mockFhirService
                .Setup(s => s.CreateObservationAsync(It.IsAny<Observation>()))
                .ReturnsAsync(created);

            var result = await _controller.CreateObservation(request);

            Assert.IsInstanceOfType(result.Result, typeof(CreatedAtActionResult));
        }

        [TestMethod]
        public async Task UpdateObservation_ReturnsOk()
        {
            var obs = new Observation { Id = "up1" };

            _mockFhirService.Setup(s => s.UpdateObservationAsync(It.IsAny<Observation>())).ReturnsAsync(obs);

            var result = await _controller.UpdateObservation("up1", obs);

            Assert.IsInstanceOfType(result.Result, typeof(OkObjectResult));
        }

        [TestMethod]
        public async Task DeleteObservation_ReturnsNoContent()
        {
            _mockFhirService.Setup(s => s.DeleteObservationAsync("d1")).Returns(Task.CompletedTask);

            var result = await _controller.DeleteObservation("d1");

            Assert.IsInstanceOfType(result, typeof(NoContentResult));
        }

        [TestMethod]
        public async Task DeleteObservation_ReturnsNotFound()
        {
            _mockFhirService
                .Setup(s => s.DeleteObservationAsync(It.IsAny<string>()))
                .ThrowsAsync(new Hl7.Fhir.Rest.FhirOperationException("Missing", System.Net.HttpStatusCode.NotFound));

            var result = await _controller.DeleteObservation("missing");

            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }
    }
}
