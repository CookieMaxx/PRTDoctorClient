using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7Patient = Hl7.Fhir.Model.Patient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using Hl7.Fhir.Serialization;
using System.Windows.Input;
using Hl7.Fhir.ElementModel;

namespace PRTDoctorClient
{
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
                ShowLoading(true);
                Debug.WriteLine("Starting to load patients from FHIR server...");
                var client = new FhirClient(_fhirServerUrl);

                var searchParams = new SearchParams()
                    .Add("_sort", "-_lastUpdated") // Sorting by _lastUpdated in descending order
                    .LimitTo(2); // Limiting to 10 results

                var searchResult = await client.SearchAsync<Hl7Patient>(searchParams);

                var patients = new List<PatientDisplay>();
                foreach (var entry in searchResult.Entry)
                {
                    if (entry.Resource is Hl7Patient patient)
                    {
                        try
                        {
                            // Skip patients with invalid birth dates
                            if (patient.BirthDateElement != null && !IsValidDate(patient.BirthDateElement.ToString()))
                            {
                                Debug.WriteLine($"Skipped patient with invalid birth date: {patient.ToJson()}");
                                continue;
                            }

                            var displayPatient = new PatientDisplay
                            {
                                Name = patient.Name?.FirstOrDefault()?.ToString() ?? "Unnamed",
                                FullPatient = patient
                            };

                            patients.Add(displayPatient);
                            Debug.WriteLine($"Fetched patient: {displayPatient.Name}");
                        }
                        catch (StructuralTypeException ex)
                        {
                            Debug.WriteLine($"Error parsing patient data: {ex.Message}");
                            Debug.WriteLine($"Problematic Patient Resource: {patient.ToJson()}");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Unexpected error: {ex.Message}");
                            Debug.WriteLine($"Problematic Patient Resource: {patient.ToJson()}");
                        }
                    }
                }

                listPatients.ItemsSource = patients;
                Debug.WriteLine("Patients loaded into ListBox");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error fetching patients: {ex.Message}");
            }
            finally
            {
                ShowLoading(false);
            }
        }

        private bool IsValidDate(string date)
        {
            DateTime parsedDate;
            return DateTime.TryParse(date, out parsedDate);
        }


        private bool IsValidPatient(Hl7Patient patient)
        {
            if (!string.IsNullOrEmpty(patient.BirthDate))
            {
                DateTime parsedBirthDate;
                if (!DateTime.TryParse(patient.BirthDate, out parsedBirthDate))
                {
                    return false; // Invalid birth date
                }
            }
            return true; // Patient is valid
        }




        private int _loadingCount = 0;

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
                listPatients.Visibility = Visibility.Collapsed;
                lstMeetingsDate.Visibility = Visibility.Collapsed;
            }
            else
            {
                LoadingPanel.Visibility = Visibility.Collapsed;
                listPatients.Visibility = Visibility.Visible;
                lstMeetingsDate.Visibility = Visibility.Visible;
            }
        }

        private void DisplayAppointmentsForToday()
        {
            DateTime selectedDate = DateTime.Today;
            LoadMeetings(selectedDate);
        }

        private async void LoadMeetings(DateTime date)
        {
            try
            {
                ShowLoading(true);
                var client = new FhirClient(_fhirServerUrl);
                var searchResult = await client.SearchAsync<Appointment>(new string[] { $"date=ge{date:yyyy-MM-dd}", $"date=le{date:yyyy-MM-dd}" });

                var meetings = searchResult.Entry
                    .Where(e => e.Resource is Appointment)
                    .Select(e => (Appointment)e.Resource)
                    .Select(appt => new MeetingDisplay
                    {
                        Id = appt.Id,
                        Date = appt.Start.HasValue ? appt.Start.Value.DateTime.ToString("yyyy-MM-dd") : "Unknown",
                        Time = appt.Start.HasValue ? appt.Start.Value.DateTime.ToString("HH:mm") : "Unknown",
                        Description = appt.Description ?? "No Description",
                        Priority = appt.Priority == 1 ? "High" : appt.Priority == 2 ? "Medium" : "Low",
                        Status = appt.Status.HasValue ? appt.Status.ToString() : "Unknown",
                        StatusColor = appt.Status.HasValue ? GetStatusColor(appt.Status.Value) : "Black",
                        PatientId = appt.Participant?.FirstOrDefault()?.Actor?.Reference?.Split('/').Last()
                    })
                    .ToList();

                if (meetings.Count == 0)
                {
                    meetings.Add(new MeetingDisplay { Description = "No appointments today", Date = string.Empty, Time = string.Empty, Priority = string.Empty, Status = string.Empty, StatusColor = "Black" });
                }

                lstMeetingsDate.ItemsSource = meetings;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error fetching meetings: {ex.Message}");
                lstMeetingsDate.ItemsSource = new List<MeetingDisplay> { new MeetingDisplay { Description = "Error loading meetings", Date = string.Empty, Time = string.Empty, Priority = string.Empty, Status = string.Empty, StatusColor = "Red" } };
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

        private void lstMeetingsDate_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var selectedMeeting = (MeetingDisplay)lstMeetingsDate.SelectedItem;
            if (selectedMeeting != null && selectedMeeting.Description != "No appointments today")
            {
                var client = new FhirClient(_fhirServerUrl);
                var patient = client.Read<Hl7Patient>(new Uri($"Patient/{selectedMeeting.PatientId}", UriKind.Relative));
                var meetingWindow = new MeetingSchedule(patient, selectedMeeting);
                meetingWindow.Show();
            }
        }

        private void PreviousDay_Click(object sender, RoutedEventArgs e)
        {
            // Implementation for previous day button click
        }

        public class PatientDisplay
        {
            public string Name { get; set; }
            public Hl7Patient FullPatient { get; set; }
        }
    }
}
