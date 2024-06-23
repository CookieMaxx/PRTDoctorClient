using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using Hl7Patient = Hl7.Fhir.Model.Patient;

namespace PRTDoctorClient
{
    public partial class PatientStatistic : Window
    {
        private Hl7Patient _patient;

        public PatientStatistic(Hl7Patient patient)
        {
            InitializeComponent();
            _patient = patient;
            LoadPatientWellbeing();
        }

        private async void LoadPatientWellbeing()
        {
            // Load and display patient info
            var patientInfo = new List<PatientInfoDisplay>
            {
                new PatientInfoDisplay { Field = "Start of Mental Health Treatment", Value = "01/01/2023" },
                new PatientInfoDisplay { Field = "Start of Medication intake", Value = "01/01/2023" },
                new PatientInfoDisplay { Field = "Last meeting date", Value = "01/01/2024" },
                new PatientInfoDisplay { Field = "Last intake of medication", Value = "01/02/2024" },
                new PatientInfoDisplay { Field = "Last info about wellbeing", Value = "Good" }
            };
            listPatientInfo.ItemsSource = patientInfo;

            // Load survey responses to calculate wellbeing scores
            var client = new FhirClient("http://hapi.fhir.org/baseR4");
            var searchResult = await client.SearchAsync<QuestionnaireResponse>(new string[] { $"subject=Patient/{_patient.Id}" });

            var wellbeingScores = new List<int>();
            var dates = new List<DateTime>();

            foreach (var entry in searchResult.Entry)
            {
                if (entry.Resource is QuestionnaireResponse questionnaireResponse)
                {
                    // Process the scores based on survey responses
                    int score = CalculateWellbeingScore(questionnaireResponse);
                    wellbeingScores.Add(score);
                    dates.Add(questionnaireResponse.Authored?.Value ?? DateTime.Now);
                }
            }

            // Display the wellbeing chart
            DisplayWellbeingChart(wellbeingScores, dates);
        }

        private int CalculateWellbeingScore(QuestionnaireResponse questionnaireResponse)
        {
            // Custom logic to calculate the score based on survey responses
            return new Random().Next(0, 100); // Replace with actual calculation
        }

        private void DisplayWellbeingChart(List<int> scores, List<DateTime> dates)
        {
            if (scores.Count == 0 || dates.Count == 0)
            {
                MessageBox.Show("No wellbeing data available.");
                return;
            }

            double chartHeight = WellbeingChart.ActualHeight;
            double chartWidth = WellbeingChart.ActualWidth;
            double maxScore = 100;
            double minScore = 0;

            double xInterval = chartWidth / (scores.Count - 1);
            double yRatio = chartHeight / (maxScore - minScore);

            for (int i = 0; i < scores.Count - 1; i++)
            {
                Line line = new Line
                {
                    X1 = i * xInterval,
                    Y1 = chartHeight - (scores[i] - minScore) * yRatio,
                    X2 = (i + 1) * xInterval,
                    Y2 = chartHeight - (scores[i + 1] - minScore) * yRatio,
                    Stroke = Brushes.Blue,
                    StrokeThickness = 2
                };
                WellbeingChart.Children.Add(line);
            }
        }
    }

    public class PatientInfoDisplay
    {
        public string Field { get; set; }
        public string Value { get; set; }
    }
}
