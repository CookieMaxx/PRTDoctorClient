using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using System;
using System.Windows;
using static PRTDoctorClient.PatientWindow;
using System.Windows.Controls;

namespace PRTDoctorClient
{
    public partial class MeetingSchedule : Window
    {
        private Patient _patient;
        private MeetingDisplay _meeting;

        public MeetingSchedule(Patient patient, MeetingDisplay meeting = null)
        {
            InitializeComponent();
            _patient = patient;
            _meeting = meeting;

            PopulateTimeSlots();

            if (_meeting != null)
            {
                // Populate the fields with existing meeting details
                datePicker.SelectedDate = DateTime.Parse(_meeting.Date);
                timeSlotComboBox.SelectedItem = _meeting.Time;
                comboBoxPriority.SelectedItem = _meeting.Priority;
                descriptionTextBox.Text = _meeting.Description;
            }
        }

        private void PopulateTimeSlots()
        {
            var timeSlots = new string[]
            {
                "08:00", "08:30", "09:00", "09:30", "10:00", "10:30", "11:00", "11:30",
                "12:00", "12:30", "13:00", "13:30", "14:00", "14:30", "15:00", "15:30",
                "16:00", "16:30"
            };

            foreach (var timeSlot in timeSlots)
            {
                timeSlotComboBox.Items.Add(timeSlot);
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedDate = datePicker.SelectedDate;
            var selectedTime = timeSlotComboBox.SelectedItem as string;
            var selectedPriority = ((ComboBoxItem)comboBoxPriority.SelectedItem)?.Content.ToString();
            var description = descriptionTextBox.Text;
            var selectedStatus = ((ComboBoxItem)statusComboBox.SelectedItem)?.Content.ToString().Replace(" ", "").ToLower();

            if (selectedDate != null && !string.IsNullOrEmpty(selectedTime) && !string.IsNullOrEmpty(selectedPriority) && !string.IsNullOrEmpty(selectedStatus))
            {
                var client = new FhirClient("http://hapi.fhir.org/baseR4");

                Appointment appointment;
                try
                {
                    if (_meeting == null)
                    {
                        appointment = new Appointment();
                    }
                    else
                    {
                        appointment = await client.ReadAsync<Appointment>($"Appointment/{_meeting.Id}");
                    }

                    appointment.Status = Enum.Parse<Appointment.AppointmentStatus>(selectedStatus, true);
                    appointment.Start = DateTimeOffset.Parse($"{selectedDate.Value:yyyy-MM-dd}T{selectedTime}:00+00:00");
                    appointment.End = appointment.Start.Value.AddHours(1); // Set end time one hour later
                    appointment.Priority = selectedPriority == "High" ? 1 : selectedPriority == "Medium" ? 2 : 3;
                    appointment.Description = description;

                    // Clear existing participants and add the patient as a participant
                    appointment.Participant.Clear();
                    appointment.Participant.Add(new Appointment.ParticipantComponent
                    {
                        Actor = new ResourceReference($"Patient/{_patient.Id}")
                    });

                    // Debugging information
                    Console.WriteLine($"Appointment ID: {_meeting?.Id}");
                    Console.WriteLine($"Start: {appointment.Start}");
                    Console.WriteLine($"End: {appointment.End}");
                    Console.WriteLine($"Priority: {appointment.Priority}");
                    Console.WriteLine($"Description: {appointment.Description}");
                    Console.WriteLine($"Participant: {appointment.Participant[0].Actor.Reference}");

                    if (_meeting == null)
                    {
                        var createdAppointment = await client.CreateAsync(appointment);
                        Console.WriteLine($"Created Appointment ID: {createdAppointment.Id}");
                    }
                    else
                    {
                        var updatedAppointment = await client.UpdateAsync(appointment);
                        Console.WriteLine($"Updated Appointment ID: {updatedAppointment.Id}");
                    }

                    MessageBox.Show("Appointment saved successfully.");
                    this.Close(); // Close the window after saving
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving appointment: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("Please fill out all fields.");
            }
        }


    }
}
