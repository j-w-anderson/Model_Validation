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
using System.IO;
using Microsoft.Win32;
using System.Text.RegularExpressions;

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
        List<DataScan> ds_list = new List<DataScan>();
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
            ds_list.Clear();
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.DefaultExt = ".asc";
            ofd.Filter = "Ascii Files (*.asc)|*.asc|Text Files (*.txt)|*.txt|W2CAD Files (*.cdp)|*.cdp";
            if (ofd.ShowDialog() == true)
            {
                // read all scans
                Regex newscan_re = new Regex("^\\$STOM");
                Regex fieldsize_re = new Regex("^%FLSZ");
                Regex scantype_re = new Regex("^%TYPE");
                Regex pnts_re = new Regex("^%PNTS");
                Regex step_re = new Regex("^%STEP");
                Regex depth_re = new Regex("^%DPTH");
                Regex data_re = new Regex("^<");
                
                foreach (string line in File.ReadAllLines(ofd.FileName))
                {
                    if (newscan_re.IsMatch(line))
                    {
                        //if (cur_scan != null) { ds_list.Add(cur_scan); }
                        ds_list.Add(new DataScan());
                    }
                    else if (fieldsize_re.IsMatch(line))
                    {
                        //char[] delimiters = { ' ', '*' };
                        string[] tokens = line.Split(' ', '*');
                        ds_list.Last().FieldX = Convert.ToDouble(tokens[1]);
                        ds_list.Last().FieldY = Convert.ToDouble(tokens[2]);
                    }
                    else if (scantype_re.IsMatch(line))
                    {
                        switch(line.Split(' ').Last())
                        {
                            case "OPP":
                                ds_list.Last().axisDir = "X";
                                break;
                            case "OPD":
                                ds_list.Last().axisDir = "Z";
                                break;
                            case "DPR":
                                ds_list.Last().axisDir = "X";
                                break;
                        }
                    }
                    else if (pnts_re.IsMatch(line))
                    {
                        ds_list.Last().scanLength = Convert.ToInt32(line.Split(' ').Last());
                    }
                    else if (step_re.IsMatch(line))
                    {
                        ds_list.Last().stepSize = Convert.ToInt32(line.Split(' ').Last());
                    }
                    else if (depth_re.IsMatch(line))
                    {
                        ds_list.Last().depth = Convert.ToInt32(line.Split(' ').Last());
                    }
                    else if (data_re.IsMatch(line))
                    {
                        double pos = ds_list.Last().axisDir == "X" ?
                            Convert.ToDouble(line.Split(' ').First().Trim('<')) :
                            Convert.ToDouble(line.Split(' ')[2]);
                        double val = Convert.ToDouble(line.Split(' ').Last().Trim('>'));
                        ds_list.Last().scan_data.Add(new Tuple<double, double>(pos, val));
                    }
                }
                prevScans_sp.Children.Clear();
                foreach(DataScan ds in ds_list)
                {
                    Label lbl = new Label();
                    string scan_type = ds.axisDir == "X" ? "Profile" : "PDD";
                    string depth_type = ds.axisDir == "X" ? ds.depth.ToString() : "NA";
                    lbl.Content = String.Format("{0} -FLSZ ({1} x {2}) - Depth ({3})",
                        scan_type, ds.FieldX, ds.FieldY, depth_type);
                    prevScans_sp.Children.Add(lbl);
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            app.ClosePatient();
            app.Dispose();
        }

        public class DataScan
        {
            public double FieldX { get; set; }
            public double FieldY { get; set; }
            public double StartPos { get; set; }
            public double stepSize { get; set; }
            public int scanLength { get; set; }
            public string axisDir { get; set; }
            public double depth { get; set; }
            public List<Tuple<double, double>> scan_data { get; set; }
            public DataScan()
            {
                scan_data = new List<Tuple<double, double>>();
            }
        }
    }
}