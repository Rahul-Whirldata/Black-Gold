using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace test_COApp
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class introductionPage : ContentPage
    {
        public introductionPage()
        {
            InitializeComponent();
        }

        async private void Button_Clicked_login(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new loginPage());
        }

        async private void Button_Clicked_register(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new registerPage());
        }
    }
}