using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRTDoctorClient
{
    public class FhirService
    {
        private readonly FhirClient _client;

        public FhirService()
        {
            _client = new FhirClient("http://hapi.fhir.org/baseR4");
            _client.Settings.PreferredFormat = ResourceFormat.Json;
        }

        public Bundle GetPatients()
        {
            return _client.Search<Patient>();
        }

        public Bundle GetObservations(string patientId)
        {
            var query = new SearchParams().Where($"patient={patientId}");
            return _client.Search<Observation>(query);
        }
    }
}
