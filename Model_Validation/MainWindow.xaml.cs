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
            Course_cb.ItemsSource = pat.Courses.Select(x => x.Id);
            Status_tb.Text = $"Patient id \"{Pat_ID_tb.Text}\" loaded.";
        }

        private void Course_cb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Status_tb.Text = "";
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
            Status_tb.Text = "";
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
                        switch (line.Split(' ').Last())
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
                foreach (DataScan ds in ds_list)
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

        int scan_num = 0;
        private void compare_btn_Click(object sender, RoutedEventArgs e)
        {
            Status_tb.Text = "";
            if (Plan_cb.SelectedIndex == -1 || ds_list.Count() == 0) { return; }
            PlanSetup ps = c.PlanSetups.Single(x => x.Id == Plan_cb.SelectedItem.ToString());
            List<ScanCompare> sc = new List<ScanCompare>();
            bool scan_found = false;
            int ds_num = 0;
            foreach (DataScan ds in ds_list)
            {
                if (scan_found) { break; }
                //sc.Clear();
                bool prof = ds.axisDir == "X";
                //find the field for this scan
                Beam beam = null;
                foreach (Beam b in ps.Beams)
                {
                    double x1 = b.ControlPoints.First().JawPositions.X1;
                    double x2 = b.ControlPoints.First().JawPositions.X2;
                    double y1 = b.ControlPoints.First().JawPositions.Y1;
                    double y2 = b.ControlPoints.First().JawPositions.Y2;
                    double xjaw = Math.Abs(x1 - x2);
                    double yjaw = Math.Abs(y1 - y2);
                    if (xjaw - ds.FieldX < 0.1 && yjaw - ds.FieldY < 0.1)
                    {
                        beam = b;
                        break;
                    }
                }
                if (beam != null)
                {
                    if (scan_num == ds_num)
                    {
                        scan_found = true;
                        scan_tb.Text = $"Comparing Scan ({ds.FieldX}x{ds.FieldY})-({ds.depth}).";
                        sc.Clear();
                    }
                    //get dose profile
                    VVector start = new VVector();
                    start.x = prof ? ds.scan_data.First().Item1 : 0;
                    start.y = prof ? ds.depth - 200 : ds.scan_data.First().Item1 - 200;
                    start.z = 0;//no inline scans
                    VVector end = new VVector();
                    end.x = prof ? ds.scan_data.Last().Item1 : 0;
                    end.y = prof ? start.y : ds.scan_data.Last().Item1 - 200;
                    double[] size = new double[ds.scanLength];
                    DoseProfile dp = beam.Dose.GetDoseProfile(start, end, size);
                    double norm_factor = prof ? dp.First(o => o.Position.x >= 0).Value : dp.Max(o => o.Value);
                    for (int i = 0; i < dp.Count(); i++)
                    {
                        sc.Add(new ScanCompare
                        {
                            measured_pos = ds.scan_data[i].Item1,
                            measured_dos = ds.scan_data[i].Item2,
                            calc_pos = prof ? dp[i].Position.x : dp[i].Position.y + 200,
                            calc_dos = dp[i].Value / norm_factor * 100
                        });
                        sc.Last().gamma = GetGamma(ds, dp, i, norm_factor, prof);
                    }

                }
                ds_num++;
            }
            if (!scan_found) { Status_tb.Text = "No matching scans/field pairs found."; return; }
            scan_cnv.Children.Clear();
            plot_scans(sc);

        }


        private double GetGamma(DataScan ds, DoseProfile dp, int i, double norm_factor, bool prof)
        {
            double dd = 2;//%
            double dta = 2;//mm
            double ref_pos = ds.scan_data[i].Item1;
            double ref_dos = ds.scan_data[i].Item2;
            int start = (int)Math.Max(0, i - 10 * dta);
            int stop = (int)Math.Min(ds.scan_data.Count() - 1, i + 10 * dta);
            List<double> gamma_values = new List<double>();
            for (double index = start; index < stop - 1; index += 0.1)
            {
                //y = y0 + (x-x0) * (y1-y0)/(x1-x0)
                int x0 = (int)Math.Floor(index);
                int x1 = (int)Math.Ceiling(index);
                double yp0 = prof ? dp[x0].Position.x : dp[x0].Position.y + 200;
                double yp1 = prof ? dp[x1].Position.x : dp[x1].Position.y + 200;
                double yd0 = dp[x0].Value / norm_factor * 100;
                double yd1 = dp[x1].Value / norm_factor * 100;
                double dos = 0; double pos = 0;
                if (x0 == x1)
                {
                    dos = yd0;
                    ref_pos = yp0;
                }
                else
                {
                    dos = yd0 + (index - x0) * (yd1 - yd0) / (x1 - x0);
                    pos = yp0 + (index - x0) * (yp1 - yp0) / (x1 - x0);
                }
                double gamma = Math.Sqrt(Math.Pow((ref_pos - ref_pos) / dta, 2) + Math.Pow((dos - ref_dos) / dd, 2));
                gamma_values.Add(gamma);
            }
            return gamma_values.Min();
        }

        private void plot_scans(List<ScanCompare> sc)
        {

            // Calculate multipliers for scaling DVH to canvas.
            //.DoseValuePresentation = DoseValuePresentation.Absolute;

            double xCoeff = scan_cnv.ActualWidth / (sc.Max(o => o.measured_pos) * 2);
            double yCoeff = scan_cnv.ActualHeight / (sc.Max(o => o.measured_dos));
            double xCoeff_gamma = xCoeff;
            double yCoeff_gamma = scan_cnv.ActualHeight / sc.Max(o => o.gamma);


            // Set X axis label
            //DoseMaxLabel.Content = string.Format("{0}", ps.Dose.DoseMax3D);
            //plot(sc,y, scan_cnv,SolidColorBrush color,xCoeff,yCoeff)
            // Draw histogram 
            for (int i = 0; i < sc.Count() - 1; i++)
            {
                // Set drawing line parameters
                var line1 = new Line() { Stroke = Brushes.Blue, StrokeThickness = 2.0 };

                // Set line coordinates
                line1.X1 = (sc[i].calc_pos - sc.Min(o => o.measured_pos)) * xCoeff;
                line1.X2 = (sc[i + 1].calc_pos - sc.Min(o => o.measured_pos)) * xCoeff;

                // Y axis start point is top-left corner of window, convert it to bottom-left.
                line1.Y1 = scan_cnv.ActualHeight - sc[i].measured_dos * yCoeff;
                line1.Y2 = scan_cnv.ActualHeight - sc[i + 1].measured_dos * yCoeff;

                // Add line to the existing canvas
                scan_cnv.Children.Add(line1);

                // Set drawing line parameters
                var line2 = new Line() { Stroke = Brushes.Red, StrokeThickness = 2.0 };

                // Set line coordinates
                line2.X1 = (sc[i].calc_pos - sc.Min(o => o.measured_pos)) * xCoeff;
                line2.X2 = (sc[i + 1].calc_pos - sc.Min(o => o.measured_pos)) * xCoeff;

                // Y axis start point is top-left corner of window, convert it to bottom-left.
                line2.Y1 = scan_cnv.ActualHeight - sc[i].calc_dos * yCoeff;
                line2.Y2 = scan_cnv.ActualHeight - sc[i + 1].calc_dos * yCoeff;

                // Add line to the existing canvas
                scan_cnv.Children.Add(line2);

                // Set drawing line parameters

                var line3 = new Line() { Stroke = Brushes.Green, StrokeThickness = 2.0 };
                // Set line coordinates
                line3.X1 = (sc[i].calc_pos - sc.Min(o => o.measured_pos)) * xCoeff;
                line3.X2 = (sc[i + 1].calc_pos - sc.Min(o => o.measured_pos)) * xCoeff;

                // Y] axis start point is top-left corner of window, convert it to bottom-left.
                line3.Y1 = scan_cnv.ActualHeight - sc[i].gamma * yCoeff_gamma;
                line3.Y2 = scan_cnv.ActualHeight - sc[i + 1].gamma * yCoeff_gamma;

                // Add line to the existing canvas
                scan_cnv.Children.Add(line3);
            }


        }
        private void prev_btn_Click(object sender, RoutedEventArgs e)
        {
            scan_num = (scan_num - 1) % ds_list.Count();
            compare_btn_Click(null, null);
        }

        private void next_btn_Click(object sender, RoutedEventArgs e)
        {

            scan_num = (scan_num + 1) % ds_list.Count();
            compare_btn_Click(null, null);
        }
    }
}