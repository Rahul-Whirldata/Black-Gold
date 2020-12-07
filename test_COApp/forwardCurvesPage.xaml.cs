using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;


namespace test_COApp
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public class GraphModel
    {
        public string category { get; set; }
        public double settle { get; set; }
    }
    public class GraphViewModel
    {
        public ObservableCollection<GraphModel> GraphView { get; set; }
    }
    public class json
    {
        public string base_date { get; set; }
        public string lookForward { get; set; }
        public List<string> compare_dates { get; set; }
        public string is_settle { get; set; }
        public string monthOf { get; set; }
        public string type { get; set; }

    }
    public partial class forwardCurvesPage : ContentPage
    {
        async private void DataProvider(json inputjson, int Id)
        {
            var values = inputjson;
            var client = new HttpClient();
            client.BaseAddress = new Uri("http://192.168.1.169:5000");

            string jsondata = JsonConvert.SerializeObject(values);
            var content = new StringContent(jsondata, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync("/test", content);

            var json = await response.Content.ReadAsStringAsync();

            string transformedJson = @"{'GraphView':" + json + "}";

            var jsonDataCollection = JsonConvert.DeserializeObject<GraphViewModel>(transformedJson);
            if (Id == 1)
            {
                chart1.ItemsSource = jsonDataCollection.GraphView;
            } else if (Id == 2)
            {
                chart2.ItemsSource = jsonDataCollection.GraphView;
            } else if (Id == 3)
            {
                chart1.ItemsSource = jsonDataCollection.GraphView;
                chart2.ItemsSource = jsonDataCollection.GraphView;
            }
        }

        public forwardCurvesPage()
        {
            InitializeComponent();
            base_date.Date = new DateTime(2020, 10, 8);

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
                    compare_dates = new List<string>() { },
                    is_settle = "true",
                    monthOf = pickerNum1.SelectedItem.ToString(),
                    type = pickType
                };

                DataProvider(val, 1);
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
                    compare_dates = new List<string>() { },
                    is_settle = "true",
                    monthOf = pickerNum2.SelectedItem.ToString(),
                    type = pickType
                };

                DataProvider(val, 2);
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
                    compare_dates = new List<string>() { },
                    is_settle = "true",
                    monthOf = pickerNum1.SelectedItem.ToString(),
                    type = pickType
                };
                DataProvider(val, 1);
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
                    compare_dates = new List<string>() { },
                    is_settle = "true",
                    monthOf = pickerNum2.SelectedItem.ToString(),
                    type = pickType
                };
                DataProvider(val, 2);
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
                    compare_dates = new List<string>() { },
                    is_settle = "true",
                    monthOf = pickerNum1.SelectedItem.ToString(),
                    type = pickType
                };
                DataProvider(val, 1);
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
                    compare_dates = new List<string>() { },
                    is_settle = "true",
                    monthOf = pickerNum2.SelectedItem.ToString(),
                    type = pickType
                };
                DataProvider(val, 2);
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
    }
}