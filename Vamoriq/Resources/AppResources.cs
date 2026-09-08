namespace Vamoriq.Resources
{

    public static class AppResources
    {
        #region Strings

        public const string Back = "Back";
        public const string Home = "Home";
        public const string Settings = "Settings";
        public const string Favorites = "Favorites";
        public const string History = "History";
        public const string Community = "Community";
        public const string Analytics = "Analytics";
        public const string Achievements = "Achievements";
        public const string Support = "Support";
        public const string Share = "Share";
        public const string Refresh = "Refresh";
        public const string Menu = "Menu";

        public const string Login = "Login";
        public const string Register = "Register";
        public const string Logout = "Logout";
        public const string Email = "Email";
        public const string Password = "Password";
        public const string ConfirmPassword = "Confirm Password";
        public const string ForgotPassword = "Forgot Password?";
        public const string SignIn = "Sign In";
        public const string SignUp = "Sign Up";
        public const string ContinueAsGuest = "Continue as guest";
        public const string Welcome = "Welcome!";
        public const string DiscoverRecipes = "Discover amazing cocktail recipes based on your ingredients";
        public const string GetStarted = "Get Started";
        public const string DrinkResponsibly = "18+ Only | Please drink responsibly";

        public const string Recipe = "Recipe";
        public const string RecipeDetails = "Recipe Details";
        public const string ShareRecipe = "Share Recipe";
        public const string Ingredients = "Ingredients";
        public const string Instructions = "Instructions";
        public const string AddToFavorites = "Add to Favorites";
        public const string RemoveFromFavorites = "Remove from Favorites";
        public const string GenerateRecipe = "Generate Recipe";
        public const string EnterIngredients = "Enter your ingredients...";
        public const string NoIngredients = "No ingredients entered";
        public const string LoadingRecipe = "Generating your recipe...";
        public const string RecipeGenerated = "Recipe generated successfully!";
        public const string RecipeError = "Failed to generate recipe. Please try again.";

        public const string Loading = "Loading...";
        public const string Error = "Error";
        public const string Success = "Success";
        public const string Warning = "Warning";
        public const string Info = "Information";
        public const string Ok = "OK";
        public const string Cancel = "Cancel";
        public const string Save = "Save";
        public const string Delete = "Delete";
        public const string Edit = "Edit";
        public const string Add = "Add";
        public const string Remove = "Remove";
        public const string Search = "Search";
        public const string Filter = "Filter";
        public const string Sort = "Sort";

        public const string InvalidEmail = "Please enter a valid email address";
        public const string PasswordTooShort = "Password must be at least 8 characters long";
        public const string PasswordMismatch = "Passwords do not match";
        public const string RequiredField = "This field is required";
        public const string InvalidInput = "Invalid input";
        public const string NetworkError = "Network error. Please check your connection";
        public const string ServerError = "Server error. Please try again later";

        #endregion

        #region Colors

        public const string PrimaryColor = "Primary";
        public const string BackgroundColor = "Background";
        public const string SurfaceColor = "Surface";
        public const string OnBackgroundColor = "OnBackground";
        public const string OnSurfaceColor = "OnSurface";
        public const string OnPrimaryColor = "OnPrimary";
        public const string OutlineColor = "Outline";
        public const string ErrorColor = "Error";
        public const string SuccessColor = "Success";
        public const string WarningColor = "Warning";

        #endregion

        #region Font Sizes

        public const double TitleFontSize = 28;
        public const double SubtitleFontSize = 18;
        public const double BodyFontSize = 16;
        public const double CaptionFontSize = 14;
        public const double DisclaimerFontSize = 12;

        #endregion

        #region Spacing

        public const double SmallSpacing = 8;
        public const double MediumSpacing = 16;
        public const double LargeSpacing = 24;
        public const double ExtraLargeSpacing = 32;

        #endregion

        #region Margins

        public const double SmallMargin = 8;
        public const double MediumMargin = 16;
        public const double LargeMargin = 24;
        public const double ExtraLargeMargin = 32;

        #endregion

        #region Corner Radius

        public const double SmallCornerRadius = 8;
        public const double MediumCornerRadius = 12;
        public const double LargeCornerRadius = 16;
        public const double ExtraLargeCornerRadius = 20;

        #endregion

        #region Animation Durations

        public const int FastAnimation = 200;
        public const int NormalAnimation = 300;
        public const int SlowAnimation = 500;

        #endregion

        #region Cache Settings

        public const int MaxCacheSize = 10000;
        public const int DefaultCacheExpirationMinutes = 30;
        public const int LongCacheExpirationMinutes = 60;

        #endregion

        #region API Settings

        public const int DefaultTimeoutSeconds = 30;
        public const int MaxRetryAttempts = 3;
        public const int RetryDelayMilliseconds = 1000;

        #endregion

        #region Validation Limits

        public const int MaxInputLength = 1000;
        public const int MaxIngredientLength = 100;
        public const int MaxRecipeNameLength = 200;
        public const int MaxDescriptionLength = 2000;
        public const int MinPasswordLength = 8;
        public const int MaxEmailLength = 254;

        #endregion
    }
}
