using Newtonsoft.Json;
using Syncfusion.SfChart.XForms;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;


namespace test_COApp
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public class GraphModel
    {
        public string category { get; set; }
        public double settle0 { get; set; }
        public double settle1 { get; set; }
        public string settle1_desc { get; set; }
        public double settle2 { set; get; }
        public string settle2_desc { get; set; }
        public double settle3 { get; set; }
        public string settle3_desc { get; set; }
        public double settle4 { get; set; }
        public string settle4_desc { get; set; }
        public double settle5 { get; set; }
        public string settle5_desc { get; set; }

    }
    public class GraphViewModel
    {
        public ObservableCollection<GraphModel> GraphView { get; set; }
    }
    public class json
    {
        public string base_date { get; set; }
        public string lookForward { get; set; }
        public ObservableCollection<string> compare_dates { get; set; }
        public string is_settle { get; set; }
        public string monthOf { get; set; }
        public string type { get; set; }

    }
    public partial class forwardCurvesPage : ContentPage
    {
        ObservableCollection<string> compareDateList = new ObservableCollection<string>();
        async private void DataProvider(json inputjson, int Id)
        {
            var values = inputjson;
            var client = new HttpClient();
            client.BaseAddress = new Uri("http://ec2-3-94-188-74.compute-1.amazonaws.com:5000");

            string jsondata = JsonConvert.SerializeObject(values);
            var content = new StringContent(jsondata, Encoding.UTF8, "application/json");

            Debug.WriteLine("content before sending api call -----> "+content);
            
            HttpResponseMessage response = await client.PostAsync("/forwardcurves", content);

            var json = await response.Content.ReadAsStringAsync();

            string transformedJson = @"{'GraphView':" + json + "}";

            var jsonDataCollection = JsonConvert.DeserializeObject<GraphViewModel>(transformedJson);
            Debug.WriteLine("jsondatacollection category ----> ");
            foreach (var i in jsonDataCollection.GraphView)
            {
                Debug.WriteLine(i.category);
                
            }

            if (Id == 1)
            {
                mainChart1.Series.Clear();
                foreach (var i in compareDateList)
                {
                    Debug.WriteLine("id 1 Compare list " + i);
                }

                for (var i = 0; i <= compareDateList.Count; i++)
                {
                    LineSeries chart1 = new LineSeries();
                    chart1.EnableAnimation = true;
                    chart1.EnableDataPointSelection = true;
                    chart1.EnableTooltip = true;
                    chart1.ItemsSource = jsonDataCollection.GraphView;
                    chart1.XBindingPath = "category";
                    chart1.YBindingPath = "settle"+i.ToString();

                    mainChart1.Series.Add(chart1);
                    Debug.WriteLine("id1 settle -----> " + "settle"+i.ToString());
                }
                
                
            } 
            else if (Id == 2)
            {
                mainChart2.Series.Clear();
                foreach (var i in compareDateList)
                {
                    Debug.WriteLine("id 1 Compare list " + i);
                }
                for (var i = 0; i <= compareDateList.Count; i++)
                {
                    LineSeries chart2 = new LineSeries();
                    chart2.EnableAnimation = true;
                    chart2.EnableDataPointSelection = true;
                    chart2.EnableTooltip = true;
                    chart2.ItemsSource = jsonDataCollection.GraphView;
                    chart2.XBindingPath = "category";
                    chart2.YBindingPath = "settle"+i.ToString();

                    mainChart2.Series.Add(chart2);
                    Debug.WriteLine("id2 settle ------> "+"settle"+i.ToString());
                }          
            } 
            else if (Id == 3)
            {
                mainChart1.Series.Clear();
                mainChart2.Series.Clear();
                for (var i = 0; i <= compareDateList.Count; i++)
                {
                    LineSeries chart1 = new LineSeries();
                    chart1.EnableAnimation = true;
                    chart1.EnableDataPointSelection = true;
                    chart1.EnableTooltip = true;
                    chart1.ItemsSource = jsonDataCollection.GraphView;
                    chart1.XBindingPath = "category";
                    chart1.YBindingPath = "settle"+i.ToString();

                    mainChart1.Series.Add(chart1);

                    LineSeries chart2 = new LineSeries();
                    chart2.EnableAnimation = true;
                    chart2.EnableDataPointSelection = true;
                    chart2.EnableTooltip = true;
                    chart2.ItemsSource = jsonDataCollection.GraphView;
                    chart2.XBindingPath = "category";
                    chart2.YBindingPath = "settle"+i.ToString();

                    mainChart2.Series.Add(chart2);

                    Debug.WriteLine("id3 settle -----> "+"settle"+i.ToString());
                }
                    
            }
            mainChart1.Legend = new ChartLegend() { OverflowMode = ChartLegendOverflowMode.Wrap };
            mainChart1.Legend.LabelStyle.TextColor = Color.Black;
            mainChart2.Legend = new ChartLegend() { OverflowMode = ChartLegendOverflowMode.Wrap };
            mainChart2.Legend.LabelStyle.TextColor = Color.Black;
        }

        public forwardCurvesPage()
        {
            Debug.WriteLine("inside constructor before initialize component");
            InitializeComponent();
            Debug.WriteLine("inside constructor after initialize component");
            base_date.Date = new DateTime(2020, 10, 8);
            //compare_dates.Date = new DateTime(2020, 10, 7);
            var pick1 = new List<string>();
            pick1.Add("Outright");
            pick1.Add("Spread");
            pick1.Add("Fly");
            picker1.ItemsSource = pick1;
            picker1.SelectedIndex = 0;

            var picknum1 = new List<string>();
            picknum1.Add("1");
            picknum1.Add("3");
            picknum1.Add("6");
            picknum1.Add("12");
            pickerNum1.ItemsSource = picknum1;
            pickerNum1.SelectedIndex = 0;

            var pick2 = new List<string>();
            pick2.Add("Outright");
            pick2.Add("Spread");
            pick2.Add("Fly");
            picker2.ItemsSource = pick2;
            picker2.SelectedIndex = 0;

            var picknum2 = new List<string>();
            picknum2.Add("1");
            picknum2.Add("3");
            picknum2.Add("6");
            picknum2.Add("12");
            pickerNum2.ItemsSource = picknum2;
            pickerNum2.SelectedIndex = 0;

            compareDateListView.ItemsSource = compareDateList;
            Debug.WriteLine("Constructor comparedatelist ------> ");
            foreach (var i in compareDateList)
            {
                Debug.WriteLine(i);
            }
        }

        private void base_date_DateChanged(object sender, DateChangedEventArgs e)
        {
            try
            {
                var baseDate = e.NewDate.ToString("yyyy-MM-dd");
                var look_forward = LookForward.Value.ToString();
                string pickType = null;
                if (picker1.SelectedIndex == 0)
                {
                    pickType = "O";
                }
                else if (picker1.SelectedIndex == 1)
                {
                    pickType = "S";
                }
                else if (picker1.SelectedIndex == 2)
                {
                    pickType = "F";
                }

                var val = new json()
                {
                    base_date = baseDate,
                    lookForward = look_forward,
                    compare_dates = compareDateList,
                    is_settle = "true",
                    monthOf = pickerNum1.SelectedItem.ToString(),
                    type = pickType
                };

                DataProvider(val, 1);
                Debug.WriteLine("base date first try statement ran !!!!!");
            }
            catch (NullReferenceException r)
            {
                Debug.WriteLine(r);
            }
            finally
            {
                Debug.WriteLine("executed without error! - date change 1");
            }

            try
            {
                var baseDate = e.NewDate.ToString("yyyy-MM-dd");
                var look_forward = LookForward.Value.ToString();
                string pickType = null;
                if (picker2.SelectedIndex == 0)
                {
                    pickType = "O";
                }
                else if (picker2.SelectedIndex == 1)
                {
                    pickType = "S";
                }
                else if (picker2.SelectedIndex == 2)
                {
                    pickType = "F";
                }

                var val = new json()
                {
                    base_date = baseDate,
                    lookForward = look_forward,
                    compare_dates = compareDateList,
                    is_settle = "true",
                    monthOf = pickerNum2.SelectedItem.ToString(),
                    type = pickType
                };

                DataProvider(val, 2);
                Debug.WriteLine("base date 2nd try statement ran !!!!!!!");
            }
            catch (NullReferenceException r)
            {
                Debug.WriteLine(r);
            }
            finally
            {
                Debug.WriteLine("executed without error! - date change 2");
            }

        }

        private void LookForward_ValueChanged(object sender, ValueChangedEventArgs e)
        {
            try
            {
                var baseDate = base_date.Date.ToString("yyyy-MM-dd");
                var look_forward = LookForward.Value.ToString();
                string pickType = null;
                if (picker1.SelectedIndex == 0)
                {
                    pickType = "O";
                }
                else if (picker1.SelectedIndex == 1)
                {
                    pickType = "S";
                }
                else if (picker1.SelectedIndex == 2)
                {
                    pickType = "F";
                }

                var val = new json()
                {
                    base_date = baseDate,
                    lookForward = look_forward,
                    compare_dates = compareDateList,
                    is_settle = "true",
                    monthOf = pickerNum1.SelectedItem.ToString(),
                    type = pickType
                };
                DataProvider(val, 1);
                Debug.WriteLine("look forward 1st try statement ran!!!!");
            }
            catch (NullReferenceException r)
            {
                Debug.WriteLine(r);
            }
            finally
            {
                Debug.WriteLine("executed without error! - lookback 1");
            }

            try
            {
                var baseDate = base_date.Date.ToString("yyyy-MM-dd");
                var look_forward = LookForward.Value.ToString();
                string pickType = null;
                if (picker2.SelectedIndex == 0)
                {
                    pickType = "O";
                }
                else if (picker2.SelectedIndex == 1)
                {
                    pickType = "S";
                }
                else if (picker2.SelectedIndex == 2)
                {
                    pickType = "F";
                }

                var val = new json()
                {
                    base_date = baseDate,
                    lookForward = look_forward,
                    compare_dates = compareDateList,
                    is_settle = "true",
                    monthOf = pickerNum2.SelectedItem.ToString(),
                    type = pickType
                };
                DataProvider(val, 2);
                Debug.WriteLine("look forward second try statement ran !!!!!");
            }
            catch (NullReferenceException r)
            {
                Debug.WriteLine(r);
            }
            finally
            {
                Debug.WriteLine("executed without error! - lookback 2");
            }
        }

        private void picker1_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                var baseDate = base_date.Date.ToString("yyyy-MM-dd");
                var look_forward = LookForward.Value.ToString();
                string pickType = null;
                if (picker1.SelectedIndex == 0)
                {
                    pickType = "O";
                } else if (picker1.SelectedIndex == 1)
                {
                    pickType = "S";
                } else if (picker1.SelectedIndex == 2)
                {
                    pickType = "F";
                }

                var val = new json()
                {
                    base_date = baseDate,
                    lookForward = look_forward,
                    compare_dates = compareDateList,
                    is_settle = "true",
                    monthOf = pickerNum1.SelectedItem.ToString(),
                    type = pickType
                };
                DataProvider(val, 1);
                Debug.WriteLine("picker 1 try statement ran !!!!!!!!!!!!!!!!");
            }
            catch (NullReferenceException r)
            {
                Debug.WriteLine(r);
            }
            finally
            {
                Debug.WriteLine("executed without error! - picker 1");
            }
        }

        private void picker2_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                var baseDate = base_date.Date.ToString("yyyy-MM-dd");
                var look_forward = LookForward.Value.ToString();
                string pickType = null;
                if (picker2.SelectedIndex == 0)
                {
                    pickType = "O";
                }
                else if (picker2.SelectedIndex == 1)
                {
                    pickType = "S";
                }
                else if (picker2.SelectedIndex == 2)
                {
                    pickType = "F";
                }

                var val = new json()
                {
                    base_date = baseDate,
                    lookForward = look_forward,
                    compare_dates = compareDateList,
                    is_settle = "true",
                    monthOf = pickerNum2.SelectedItem.ToString(),
                    type = pickType
                };
                DataProvider(val, 2);
                Debug.WriteLine("picker 2 try statement ran!!!!!!!!!!");
            }
            catch (NullReferenceException r)
            {
                Debug.WriteLine(r);
            }
            finally
            {
                Debug.WriteLine("executed without error! - picker 2");
            }
        }

        private void compareDateListView_ItemTapped(object sender, ItemTappedEventArgs e)
        {
            compareDateList.Remove(e.Item.ToString());

            try
            {
                var baseDate = base_date.Date.ToString("yyyy-MM-dd");
                var look_forward = LookForward.Value.ToString();
                string pickType = null;
                if (picker1.SelectedIndex == 0)
                {
                    pickType = "O";
                }
                else if (picker1.SelectedIndex == 1)
                {
                    pickType = "S";
                }
                else if (picker1.SelectedIndex == 2)
                {
                    pickType = "F";
                }

                var val = new json()
                {
                    base_date = baseDate,
                    lookForward = look_forward,
                    compare_dates = compareDateList,
                    is_settle = "true",
                    monthOf = pickerNum1.SelectedItem.ToString(),
                    type = pickType
                };

                DataProvider(val, 1);
                Debug.WriteLine("item tapped first try statement ran !!!!!");
            }
            catch (NullReferenceException r)
            {
                Debug.WriteLine(r);
            }
            finally
            {
                Debug.WriteLine("executed without error! - item tapped 1");
            }

            try
            {
                var baseDate = base_date.Date.ToString("yyyy-MM-dd");
                var look_forward = LookForward.Value.ToString();
                string pickType = null;
                if (picker2.SelectedIndex == 0)
                {
                    pickType = "O";
                }
                else if (picker2.SelectedIndex == 1)
                {
                    pickType = "S";
                }
                else if (picker2.SelectedIndex == 2)
                {
                    pickType = "F";
                }

                var val = new json()
                {
                    base_date = baseDate,
                    lookForward = look_forward,
                    compare_dates = compareDateList,
                    is_settle = "true",
                    monthOf = pickerNum2.SelectedItem.ToString(),
                    type = pickType
                };

                DataProvider(val, 2);
                Debug.WriteLine("item tapped 2nd try statement ran !!!!!!!");
            }
            catch (NullReferenceException r)
            {
                Debug.WriteLine(r);
            }
            finally
            {
                Debug.WriteLine("executed without error! - item tapped 2");
            }

            if (compareDateList.Count < 5)
            {
                compare_dates.IsEnabled = true;
                //Debug.WriteLine("Inside item tapped if statement...comparelist count -----> "+ compareDateList.Count);
                //Debug.WriteLine("inside item tapped if statment ... comparelist");
                //foreach (var i in compareDateList)
                //{
                //    Debug.WriteLine(i);
                //}
            }
            //Debug.WriteLine("outside item tapped if statment ... comparelist");
            //foreach (var i in compareDateList)
            //{
            //    Debug.WriteLine(i);
            //}
            
        }

        private void compare_dates_DateSelected(object sender, DateChangedEventArgs e)
        {
            compareDateList.Add(e.NewDate.ToString("yyyy-MM-dd"));

            try
            {
                var baseDate = base_date.Date.ToString("yyyy-MM-dd");
                var look_forward = LookForward.Value.ToString();
                string pickType = null;
                if (picker1.SelectedIndex == 0)
                {
                    pickType = "O";
                }
                else if (picker1.SelectedIndex == 1)
                {
                    pickType = "S";
                }
                else if (picker1.SelectedIndex == 2)
                {
                    pickType = "F";
                }

                var val = new json()
                {
                    base_date = baseDate,
                    lookForward = look_forward,
                    compare_dates = compareDateList,
                    is_settle = "true",
                    monthOf = pickerNum1.SelectedItem.ToString(),
                    type = pickType
                };

                DataProvider(val, 1);
                Debug.WriteLine("compare date first try statement ran !!!!!");
            }
            catch (NullReferenceException r)
            {
                Debug.WriteLine(r);
            }
            finally
            {
                Debug.WriteLine("executed without error! - compare date change 1");
            }

            try
            {
                var baseDate = base_date.Date.ToString("yyyy-MM-dd");
                var look_forward = LookForward.Value.ToString();
                string pickType = null;
                if (picker2.SelectedIndex == 0)
                {
                    pickType = "O";
                }
                else if (picker2.SelectedIndex == 1)
                {
                    pickType = "S";
                }
                else if (picker2.SelectedIndex == 2)
                {
                    pickType = "F";
                }

                var val = new json()
                {
                    base_date = baseDate,
                    lookForward = look_forward,
                    compare_dates = compareDateList,
                    is_settle = "true",
                    monthOf = pickerNum2.SelectedItem.ToString(),
                    type = pickType
                };

                DataProvider(val, 2);
                Debug.WriteLine("compare date 2nd try statement ran !!!!!!!");
            }
            catch (NullReferenceException r)
            {
                Debug.WriteLine(r);
            }
            finally
            {
                Debug.WriteLine("executed without error! - compare date change 2");
            }
            //Debug.WriteLine("outside compare date changed if statement...comparelist count -----> " + compareDateList.Count);
            //Debug.WriteLine("outside compare date changed if statment ... comparelist");
            //foreach (var i in compareDateList)
            //{
            //    Debug.WriteLine(i);
            //}
            if (compareDateList.Count == 5)
            {
                compare_dates.IsEnabled = false;
                //Debug.WriteLine("insode compare date changed if statement...comparelist count -----> " + compareDateList.Count);
                //Debug.WriteLine("inside compare date changed if statment ... comparelist");
                //foreach (var i in compareDateList)
                //{
                //    Debug.WriteLine(i);
                //}
            }
            
        }
    }
}