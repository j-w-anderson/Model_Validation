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
using System.Windows.Navigation;
using System.Windows.Shapes;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace Model_Validation
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        VMS.TPS.Common.Model.API.Application app = VMS.TPS.Common.Model.API.Application.CreateApplication();

        Patient pat = null;
        Course c = null;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Load_Pat_bn_Click(object sender, RoutedEventArgs e)
        {
            Status_tb.Text = $"Loading patient id \"{Pat_ID_tb.Text}\"...";
            if (string.IsNullOrEmpty(Pat_ID_tb.Text))
            {
                MessageBox.Show("Please input a patient ID before clicking load patient.");
                Status_tb.Text = "";
                return;
            }
            try
            {
                app.ClosePatient();
                pat = app.OpenPatientById(Pat_ID_tb.Text);
                if (pat == null) { throw new Exception("Pat is null"); }
            }
            catch
            {
                MessageBox.Show($"Patient ID \"{Pat_ID_tb.Text}\" was not found.");
                Status_tb.Text = "";
                return;
            }
            //if(pat == null) { throw new Exception("Valid ID but patient not loaded"); }
            //Course_cb.ItemsSource = new string[] { };
            Plan_cb.ItemsSource = new string[] { };
            Course_cb.ItemsSource = pat.Courses.Select(x=>x.Id);
            Status_tb.Text = $"Patient id \"{Pat_ID_tb.Text}\" loaded.";
        }

        private void Course_cb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Plan_cb.ItemsSource = new string[] { };
            if (Course_cb.SelectedIndex == -1) { return; }
            try
            {
                c = pat.Courses.Single(x => x.Id == Course_cb.SelectedItem.ToString());
                Plan_cb.ItemsSource = c.PlanSetups.Select(x => x.Id);
            }
            catch
            {
                throw new Exception("Error loading plan list. Possibly multiple courses with same name?");
            }
        }

        private void Plan_cb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void getScan_btn_Click(object sender, RoutedEventArgs e)
        {

        }

    }
}
