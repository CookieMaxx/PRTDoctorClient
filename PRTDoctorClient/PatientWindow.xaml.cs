using Hl7.Fhir.Model;
using Hl7Patient = Hl7.Fhir.Model.Patient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Hl7.Fhir.Rest;
using System.Windows.Input;

namespace PRTDoctorClient
{
    public partial class PatientWindow : Window
    {
        private Hl7Patient _patient;
        private Dictionary<string, string> _surveyDict = new Dictionary<string, string>();
        private int _loadingCount = 0;

        public PatientWindow(Hl7Patient patient)
        {
            InitializeComponent();
            _patient = patient;
            DataContext = this;
            LoadPatientData();
            LoadMedications();
            LoadSurveys();
            LoadPatientMedications();
            LoadPatientSurveys();
            LoadMeetings();
        }

        private void ShowLoading(bool isLoading)
        {
            if (isLoading)
            {
                _loadingCount++;
            }
            else
            {
                _loadingCount--;
            }

            if (_loadingCount > 0)
            {
                LoadingPanel.Visibility = Visibility.Visible;
                listPatientInfo.Visibility = Visibility.Collapsed;
                listMedicationView.Visibility = Visibility.Collapsed;
                listSurveyView.Visibility = Visibility.Collapsed;
                listMeetingBox.Visibility = Visibility.Collapsed;
            }
            else
            {
                LoadingPanel.Visibility = Visibility.Collapsed;
                listPatientInfo.Visibility = Visibility.Visible;
                listMedicationView.Visibility = Visibility.Visible;
                listSurveyView.Visibility = Visibility.Visible;
                listMeetingBox.Visibility = Visibility.Visible;
            }
        }

        public List<PatientInfoDisplay> PatientInfo { get; set; }

        private void LoadPatientData()
        {
            ShowLoading(true);
            PatientInfo = new List<PatientInfoDisplay>
            {
                new PatientInfoDisplay { Field = "Name", Value = _patient.Name.FirstOrDefault()?.ToString() ?? "Unnamed" },
                new PatientInfoDisplay { Field = "Birth Date", Value = _patient.BirthDate },
                new PatientInfoDisplay { Field = "Gender", Value = _patient.Gender?.ToString() ?? "Unknown" },
                new PatientInfoDisplay { Field = "Address", Value = FormatAddress(_patient.Address.FirstOrDefault()) }
            };

            listPatientInfo.ItemsSource = PatientInfo;
            ShowLoading(false);
        }

        private string FormatAddress(Address address)
        {
            if (address == null)
                return "No Address";

            return $"{address.Line?.FirstOrDefault() ?? ""} {address.City ?? ""} {address.State ?? ""} {address.PostalCode ?? ""} {address.Country ?? ""}".Trim();
        }

        private void LoadMedications()
        {
            var medications = Enum.GetValues(typeof(Medication))
                .Cast<Medication>()
                .Select(m => new SelectableItem<string> { Item = m.ToString(), IsSelected = false })
                .ToList();

            medicationListBox.ItemsSource = medications;
        }



        private void LoadSurveys()
        {
            ShowLoading(true);
            var surveys = Surveys.Select(s => new SelectableItem<string> { Item = s.Name, IsSelected = false }).ToList();
            surveyListBox.ItemsSource = surveys;
            ShowLoading(false);
        }

        private async void LoadPatientMedications()
        {
            try
            {
                var client = new FhirClient("http://hapi.fhir.org/baseR4");

                var searchResult = await client.SearchAsync<MedicationRequest>(new string[] { $"patient={_patient.Id}" });

                var medications = searchResult.Entry
                    .Where(e => e.Resource is MedicationRequest)
                    .Select(e => ((MedicationRequest)e.Resource).Medication as CodeableConcept)
                    .Select(cc =>
                    {
                        if (cc != null && cc.Coding != null && cc.Coding.Count > 0)
                        {
                            return cc.Coding.First().Display ?? cc.Text ?? "Unknown Medication";
                        }
                        return "Unknown Medication";
                    })
                    .ToList();

                listMedicationView.ItemsSource = medications;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading patient medications: {ex.Message}");
            }
        }


        private async void LoadPatientSurveys()
        {
            try
            {
                ShowLoading(true);
                var client = new FhirClient("http://hapi.fhir.org/baseR4");

                var searchResult = await client.SearchAsync<QuestionnaireResponse>(new string[] { $"subject=Patient/{_patient.Id}" });

                var surveys = new List<SurveyDisplay>();

                foreach (var entry in searchResult.Entry)
                {
                    if (entry.Resource is QuestionnaireResponse questionnaireResponse)
                    {
                        var questionnaireName = "Unknown Survey";
                        var nameExtension = questionnaireResponse.Extension.FirstOrDefault(ext => ext.Url == "http://example.org/fhir/StructureDefinition/questionnaire-response-name");
                        if (nameExtension != null && nameExtension.Value is FhirString nameValue)
                        {
                            questionnaireName = nameValue.Value;
                        }

                        var authored = questionnaireResponse.Authored?.ToString();
                        var assignedDate = !string.IsNullOrEmpty(authored) && DateTime.TryParse(authored, out var parsedDate)
                            ? parsedDate.ToString("MM/dd/yyyy")
                            : "Unknown Date";

                        var status = questionnaireResponse.Status?.ToString() ?? "Unknown Status";

                        surveys.Add(new SurveyDisplay
                        {
                            Name = questionnaireName,
                            AssignedDate = assignedDate,
                            Status = status
                        });
                    }
                }

                listSurveyView.ItemsSource = surveys;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading patient surveys: {ex.Message}");
            }
            finally
            {
                ShowLoading(false);
            }
        }

        private string FhirDateTimeToString(FhirDateTime fhirDateTime)
        {
            if (DateTime.TryParse(fhirDateTime?.Value, out var parsedDate))
            {
                return parsedDate.ToString("MM/dd/yyyy");
            }
            return "Unknown Date";
        }

        private async void AssignChanges_Click(object sender, RoutedEventArgs e)
        {
            ShowLoading(true);
            var selectedMedications = medicationListBox.Items
                .Cast<SelectableItem<string>>()
                .Where(item => item.IsSelected)
                .Select(item => item.Item)
                .ToList();

            var selectedSurveys = surveyListBox.Items
                .Cast<SelectableItem<string>>()
                .Where(item => item.IsSelected)
                .Select(item => item.Item)
                .ToList();

            await AssignMedicationsToPatient(selectedMedications);
            await AssignSurveysToPatient(selectedSurveys);
            ShowLoading(false);
        }

        private async System.Threading.Tasks.Task AssignMedicationsToPatient(List<string> medications)
        {
            var client = new FhirClient("http://hapi.fhir.org/baseR4");

            foreach (var medication in medications)
            {
                var medicationRequest = new MedicationRequest
                {
                    Subject = new ResourceReference($"Patient/{_patient.Id}"),
                    Medication = new CodeableConcept
                    {
                        Coding = new List<Coding>
                {
                    new Coding
                    {
                        System = "http://hl7.org/fhir/sid/ndc",
                        Code = medication,
                        Display = medication // Set the display value
                    }
                }
                    }
                };

                await client.CreateAsync(medicationRequest);
            }
        }

        private async System.Threading.Tasks.Task AssignSurveysToPatient(List<string> surveys)
        {
            var client = new FhirClient("http://hapi.fhir.org/baseR4");

            foreach (var surveyName in surveys)
            {
                var survey = Surveys.FirstOrDefault(s => s.Name == surveyName);
                if (survey != null)
                {
                    var questionnaireResponse = new QuestionnaireResponse
                    {
                        Status = QuestionnaireResponse.QuestionnaireResponseStatus.InProgress,
                        Authored = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssK"), // Set the authored date to now
                        Subject = new ResourceReference($"Patient/{_patient.Id}"),
                        Item = survey.Questions.Select(q => new QuestionnaireResponse.ItemComponent
                        {
                            LinkId = Guid.NewGuid().ToString(),
                            Text = q,
                            Answer = new List<QuestionnaireResponse.AnswerComponent>
                    {
                        new QuestionnaireResponse.AnswerComponent
                        {
                            Value = new FhirString("")
                        }
                    }
                        }).ToList()
                    };

                    // Add a custom extension for the name
                    questionnaireResponse.Extension.Add(new Extension
                    {
                        Url = "http://example.org/fhir/StructureDefinition/questionnaire-response-name",
                        Value = new FhirString(surveyName)
                    });

                    await client.CreateAsync(questionnaireResponse);
                }
            }
        }

        private async void LoadMeetings()
        {
            try
            {
                ShowLoading(true);
                var client = new FhirClient("http://hapi.fhir.org/baseR4");

                var searchResult = await client.SearchAsync<Appointment>(new string[] { $"patient={_patient.Id}" });

                var meetings = searchResult.Entry
                    .Where(e => e.Resource is Appointment)
                    .Select(e => (Appointment)e.Resource)
                    .Select(appt => new MeetingDisplay
                    {
                        Id = appt.Id,
                        Date = appt.Start.HasValue ? appt.Start.Value.DateTime.ToString("yyyy-MM-dd") : "Unknown",
                        Time = appt.Start.HasValue ? appt.Start.Value.DateTime.ToString("HH:mm") : "Unknown",
                        Description = appt.Description,
                        Priority = appt.Priority == 1 ? "High" : appt.Priority == 2 ? "Medium" : "Low",
                        Status = appt.Status.HasValue ? appt.Status.ToString() : "Unknown",
                        StatusColor = appt.Status.HasValue ? GetStatusColor(appt.Status.Value) : "Black"
                    })
                    .ToList();

                listMeetingBox.ItemsSource = meetings;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading patient meetings: {ex.Message}");
            }
            finally
            {
                ShowLoading(false);
            }
        }

        private string GetStatusColor(Appointment.AppointmentStatus status)
        {
            switch (status)
            {
                case Appointment.AppointmentStatus.Cancelled:
                case Appointment.AppointmentStatus.Noshow:
                case Appointment.AppointmentStatus.EnteredInError:
                    return "Red";
                case Appointment.AppointmentStatus.Proposed:
                case Appointment.AppointmentStatus.Pending:
                case Appointment.AppointmentStatus.Waitlist:
                    return "Yellow";
                case Appointment.AppointmentStatus.Booked:
                case Appointment.AppointmentStatus.Arrived:
                case Appointment.AppointmentStatus.Fulfilled:
                case Appointment.AppointmentStatus.CheckedIn:
                    return "Green";
                default:
                    return "Black"; // Default color if status is not matched
            }
        }

        private void NewMeetingButton_Click(object sender, RoutedEventArgs e)
        {
            var meetingScheduleWindow = new MeetingSchedule(_patient);
            meetingScheduleWindow.Show();
        }

        private void ChangeMeetingButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedMeeting = (MeetingDisplay)listMeetingBox.SelectedItem;
            if (selectedMeeting != null)
            {
                var meetingScheduleWindow = new MeetingSchedule(_patient, selectedMeeting);
                meetingScheduleWindow.Show();
            }
            else
            {
                MessageBox.Show("Please select a meeting to change.");
            }
        }

        private async void CancelMeetingButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedMeeting = (MeetingDisplay)listMeetingBox.SelectedItem;
            if (selectedMeeting != null)
            {
                try
                {
                    var client = new FhirClient("http://hapi.fhir.org/baseR4");
                    var appointment = await client.ReadAsync<Appointment>($"Appointment/{selectedMeeting.Id}");

                    // Set the status of the appointment to cancelled
                    appointment.Status = Appointment.AppointmentStatus.Cancelled;

                    // Update the appointment on the FHIR server
                    await client.UpdateAsync(appointment);

                    MessageBox.Show("Appointment cancelled successfully.");

                    // Refresh the meetings list to reflect the change
                    LoadMeetings();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error cancelling appointment: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("Please select a meeting to cancel.");
            }
        }

        private void ViewWellbeingButton_Click(object sender, RoutedEventArgs e)
        {
            var patientStatisticWindow = new PatientStatistic(_patient);
            patientStatisticWindow.Show();
        }


        private void ListMeetingBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var selectedMeeting = (MeetingDisplay)listMeetingBox.SelectedItem;
            if (selectedMeeting != null)
            {
                var meetingScheduleWindow = new MeetingSchedule(_patient, selectedMeeting);
                meetingScheduleWindow.Show();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        public class SurveyDisplay
        {
            public string Name { get; set; }
            public string AssignedDate { get; set; }
            public string Status { get; set; }
        }

        public class PatientInfoDisplay
        {
            public string Field { get; set; }
            public string Value { get; set; }
        }

        public class SelectableItem<T>
        {
            public T Item { get; set; }
            public bool IsSelected { get; set; }
        }

        public class Survey
        {
            public string Name { get; set; }
            public List<string> Questions { get; set; }

            public Survey(string name, List<string> questions)
            {
                Name = name;
                Questions = questions;
            }
        }

        public static List<Survey> Surveys = new List<Survey>
        {
            new Survey("Mental Health Inventory-5", new List<string>
            {
                "Are you feeling very nervous?",
                "Were you so depressed that nothing could cheer you up?",
                "Did you feel calm and collected?",
                "Did you feel down and depressed?",
                "Did you feel happy?"
            }),
            new Survey("General Health Questionnaire", new List<string>
            {
                "Have you recently felt perfectly well and in good health?",
                "Have you recently found that you are ill more frequently?",
                "Have you recently been feeling unhappy and depressed?"
            }),
            new Survey("Patient Health Questionnaire-9", new List<string>
            {
                "Over the last two weeks, how often have you been bothered by little interest or pleasure in doing things?",
                "Over the last two weeks, how often have you been bothered by feeling down, depressed, or hopeless?",
                "Over the last two weeks, how often have you been bothered by trouble falling asleep, staying asleep, or sleeping too much?"
            }),
            new Survey("Anxiety and Depression Detector", new List<string>
            {
                "I feel nervous and anxious frequently.",
                "I often feel afraid without any specific reason.",
                "I feel restless and can't stay calm.",
            })
        };
    }
}
