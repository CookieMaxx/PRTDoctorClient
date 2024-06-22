using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PRTDoctorClient
{
    /// <summary>
    /// Interaction logic for PatientStatistic.xaml
    /// </summary>
    public partial class PatientStatistic : Window
    {
        private Patient _patient;

        public PatientStatistic(Patient patient )
        {
            InitializeComponent();
            _patient = patient;
            LoadStatistics();
        }

        private void LoadStatistics()
        {
            // Load and display statistics based on the patient's medication and survey data
        }
    }
}
