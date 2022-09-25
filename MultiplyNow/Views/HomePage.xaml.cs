using System;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace MultiplyNow.Views
{
    public sealed partial class HomePage : Page
    {
        private readonly MainPage mainPage;

        private CancellationTokenSource cancellationTokenSource;

        public HomePage()
        {
            InitializeComponent();

            // do cache the state of the UI when suspending/navigating
            // this is necessary for MultiplyNow when navigating
            NavigationCacheMode = NavigationCacheMode.Required;

            SizeChanged += Page_SizeChanged;
            Loaded += Page_Loaded;

            mainPage = MainPage.CurrentMainPage;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            SetPageContentStackPanelWidth();
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SetPageContentStackPanelWidth();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // code here

            // code here
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            // code here

            // code here
        }

        private void SetPageContentStackPanelWidth()
        {
            MultiplicandTextBox.Width = MultiplierTextBox.Width = ProductTextBox.Width = ActualWidth -
                PageContentScrollViewer.Margin.Left -
                PageContentScrollViewer.Padding.Right;
        }

        #region MenuAppBarButton
        private void HomeAppBarButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            mainPage.GoToHomePage();
            mainPage.MenuNavigationListView.SelectedIndex = 0;
        }
        #endregion MenuAppBarButton

        private async void MultiplyButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            #region check multiplicand input
            string stringMultiplicand = MultiplicandTextBox.Text.Trim();
            if (string.IsNullOrEmpty(stringMultiplicand))
            {
                mainPage.NotifyUser("Multiplicand is missing.", NotifyType.ErrorMessage);
                return;
            }

            StringBuilder multiplicandStringBuilder = new StringBuilder();
            bool multiplicandCanAddDigit = false;
            bool multiplicandHasDetectedMinusSign = false;
            bool multiplicandHasMinusSign = false;
            foreach (char character in stringMultiplicand)
            {
                if (!multiplicandHasDetectedMinusSign && !multiplicandCanAddDigit)
                {
                    if (character == '-')
                    {
                        multiplicandHasMinusSign = true;
                        multiplicandHasDetectedMinusSign = true;
                    }
                }
                if (char.IsDigit(character))
                {
                    //remove leading zeroes
                    if (!multiplicandCanAddDigit)
                    {
                        if (character != '0')
                        {
                            multiplicandCanAddDigit = true;
                        }
                    }
                    if (multiplicandCanAddDigit)
                    {
                        multiplicandStringBuilder.Append(character);
                    }
                }
            }
            if (multiplicandHasMinusSign)
            {
                multiplicandStringBuilder.Insert(0, '-');
            }
            stringMultiplicand = multiplicandStringBuilder.ToString();

            if (MultiplicandTextBox.Text != stringMultiplicand)
            {
                MultiplicandTextBox.Text = stringMultiplicand;
                if (string.IsNullOrEmpty(stringMultiplicand))
                {
                    mainPage.NotifyUser("Multiplicand is missing.", NotifyType.ErrorMessage);
                    return;
                }
            }
            #endregion check multiplicand input 

            #region check multiplier input
            string stringMultiplier = MultiplierTextBox.Text.Trim();
            if (string.IsNullOrEmpty(stringMultiplier))
            {
                mainPage.NotifyUser("Multiplier is missing.", NotifyType.ErrorMessage);
                return;
            }

            StringBuilder multiplierStringBuilder = new StringBuilder();
            bool multiplierCanAddDigit = false;
            bool multiplierHasDetectedMinusSign = false;
            bool multiplierHasMinusSign = false;
            foreach (char character in stringMultiplier)
            {
                if (!multiplierHasDetectedMinusSign && !multiplierCanAddDigit)
                {
                    if (character == '-')
                    {
                        multiplierHasMinusSign = true;
                        multiplierHasDetectedMinusSign = true;
                    }
                }
                if (char.IsDigit(character))
                {
                    //remove leading zeroes
                    if (!multiplierCanAddDigit)
                    {
                        if (character != '0')
                        {
                            multiplierCanAddDigit = true;
                        }
                    }
                    if (multiplierCanAddDigit)
                    {
                        multiplierStringBuilder.Append(character);
                    }
                }
            }
            if (multiplierHasMinusSign)
            {
                multiplierStringBuilder.Insert(0, '-');
            }
            stringMultiplier = multiplierStringBuilder.ToString();

            if (MultiplierTextBox.Text != stringMultiplier)
            {
                MultiplierTextBox.Text = stringMultiplier;
                if (string.IsNullOrEmpty(stringMultiplier))
                {
                    mainPage.NotifyUser("Multiplier is missing.", NotifyType.ErrorMessage);
                    return;
                }
            }
            #endregion check multiplier input 

            if (MultiplyButton.Content.ToString() == "Multiply")
            {
                MultiplyButton.Content = "Cancel";
            }
            else
            {
                MultiplyButton.Content = "Multiply";
                if (cancellationToken.CanBeCanceled)
                {
                    cancellationTokenSource.Cancel();
                }
            }

            string stringProduct = string.Empty;

            StartProgressRing();

            ProductTextBox.Text = stringProduct;

            try
            {
                //add cancellationToken
                await Task.Run(async () => stringProduct = await DoMultiplicationAsync(stringMultiplicand, stringMultiplier), cancellationToken).ConfigureAwait(false);
            }
            catch (TaskCanceledException tcex)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    mainPage.NotifyUser(tcex.Message, NotifyType.ErrorMessage);
                    stringProduct = string.Empty;
                    ProductTextBox.Text = stringProduct;
                });
            }
            catch (Exception ex)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    mainPage.NotifyUser(ex.Message, NotifyType.ErrorMessage);
                    stringProduct = string.Empty;
                    ProductTextBox.Text = stringProduct;
                });
            }
            finally
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    MultiplyButton.Content = "Multiply";

                    ProductTextBox.Text = stringProduct;

                    ProductTextBox_SelectionChanged(null, new RoutedEventArgs());

                    StopProgressRing();
                });
            }

        }

        private Task<string> DoMultiplicationAsync(string stringMultiplicand, string stringMultiplier)
        {
            BigInteger bigIntegerMultiplicand = BigInteger.Parse(stringMultiplicand);
            BigInteger bigIntegerMultiplier = BigInteger.Parse(stringMultiplier);
            BigInteger bigIntegerProduct = bigIntegerMultiplicand * bigIntegerMultiplier;
            return Task.FromResult(bigIntegerProduct.ToString());
        }

        private void StartProgressRing()
        {
            MultiplyProgressRing.IsActive = true;
            MultiplyProgressRing.Visibility = Visibility.Visible;
        }

        private void StopProgressRing()
        {
            MultiplyProgressRing.IsActive = false;
            MultiplyProgressRing.Visibility = Visibility.Collapsed;
        }

        private void MultiplicandTextBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (MultiplicandTextBox.Text.Length == 1)
            {
                MultiplicandTextBox.Header = string.Format("Multiplicand is {0} digit.", MultiplicandTextBox.Text.Length);
            }
            else
            {
                MultiplicandTextBox.Header = string.Format("Multiplicand is {0} digits.", MultiplicandTextBox.Text.Length);
            }
        }

        private void MultiplierTextBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (MultiplierTextBox.Text.Length == 1)
            {
                MultiplierTextBox.Header = string.Format("Multiplier is {0} digit.", MultiplierTextBox.Text.Length);
            }
            else
            {
                MultiplierTextBox.Header = string.Format("Multiplier is {0} digits.", MultiplierTextBox.Text.Length);
            }
        }

        private void ProductTextBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (ProductTextBox.Text.Length == 1)
            {
                ProductTextBox.Header = string.Format("Product is {0} digit.", ProductTextBox.Text.Length);
            }
            else
            {
                ProductTextBox.Header = string.Format("Product is {0} digits.", ProductTextBox.Text.Length);
            }
        }
    }
}

