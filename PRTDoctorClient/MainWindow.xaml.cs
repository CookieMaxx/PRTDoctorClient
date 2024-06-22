using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7Patient = Hl7.Fhir.Model.Patient;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PRTDoctorClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly string _fhirServerUrl = "http://hapi.fhir.org/baseR4"; // Replace with your FHIR server URL
        private List<Patient> _fullPatientList = new List<Patient>();

        public MainWindow()
        {
            InitializeComponent();
            LoadPatientsFromFhir();
            DisplayAppointmentsForToday();
        }

        private async void LoadPatientsFromFhir()
        {
            try
            {
                var client = new FhirClient(_fhirServerUrl);

                var searchParams = new SearchParams()
                    .Add("_sort", "-_lastUpdated") // Sorting by _lastUpdated in descending order
                    .LimitTo(10); // Limiting to 10 results

                var searchResult = await client.SearchAsync<Hl7Patient>(searchParams);

                var patients = new List<PatientDisplay>();
                foreach (var entry in searchResult.Entry)
                {
                    if (entry.Resource is Hl7Patient patient)
                    {
                        var displayPatient = new PatientDisplay
                        {
                            Name = patient.Name.FirstOrDefault()?.ToString() ?? "Unnamed",
                            Age = CalculateAge(patient.BirthDateElement),
                            FullPatient = patient
                        };
                        patients.Add(displayPatient);
                        Console.WriteLine($"Fetched patient: {displayPatient.Name}, Age: {displayPatient.Age}");
                    }
                }

                listPatients.ItemsSource = patients;
                Console.WriteLine("Patients loaded into ListBox");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching patients: {ex.Message}");
            }
        }




        private void DisplayAppointmentsForToday()
        {
            DateTime selectedDate = DateTime.Today;
            LoadMeetings(selectedDate);
        }

        private async void LoadMeetings(DateTime date)
        {
            // Your logic to load meetings for the selected date from the FHIR server
            // For now, let's just add some placeholder data to lstMeetingsDate
            var meetings = new List<string>
            {
                $"Meeting 1 on {date.ToShortDateString()}",
                $"Meeting 2 on {date.ToShortDateString()}"
            };
            lstMeetingsDate.ItemsSource = meetings;
        }

        private int CalculateAge(Date birthDate)
        {
            if (birthDate == null || string.IsNullOrEmpty(birthDate.ToString())) return 0;

            var birthDateParsed = DateTime.Parse(birthDate.ToString());
            var today = DateTime.Today;
            var age = today.Year - birthDateParsed.Year;
            if (birthDateParsed.Date > today.AddYears(-age)) age--;

            return age;
        }

        private void YourListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var selectedPatientDisplay = (PatientDisplay)listPatients.SelectedItem;
            if (selectedPatientDisplay != null)
            {
                var selectedPatient = selectedPatientDisplay.FullPatient;
                var patientWindow = new PatientWindow(selectedPatient);
                patientWindow.Show();
            }
        }

        
    }

    public class PatientDisplay
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public Hl7Patient FullPatient { get; set; }
    }
}
