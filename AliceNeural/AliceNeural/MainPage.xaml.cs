using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Intent;
using AliceNeural.Utils;
using AliceNeural.Models;
using System.Text;
using System.Text.Json;
using System.Diagnostics;
using HttpProxyControl;
using Microsoft.Maui.Devices.Sensors;
using System.Net.Http.Json;
using System.Web;
using AliceNeural.Helper;

namespace AliceNeural
{
    public partial class MainPage : ContentPage
    {

        static readonly HttpClient _client = HttpProxyHelper.CreateHttpClient(setProxy: true);
        SpeechRecognizer? speechRecognizer;
        IntentRecognizer? intentRecognizerByPatternMatching;
        IntentRecognizer? intentRecognizerByCLU;
        SpeechSynthesizer? speechSynthesizer;
        TaskCompletionSourceManager<int>? taskCompletionSourceManager;
        AzureCognitiveServicesResourceManager? serviceManager;
        bool buttonToggle = false;
        Brush? buttonToggleColor;
        private static readonly JsonSerializerOptions? jsonSerializationOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
        public MainPage()
        {
            InitializeComponent();
            serviceManager = new AzureCognitiveServicesResourceManager("MyResponder", "secondDeploy");
            taskCompletionSourceManager = new TaskCompletionSourceManager<int>();
            (intentRecognizerByPatternMatching, speechSynthesizer, intentRecognizerByCLU) =
                ConfigureContinuousIntentPatternMatchingWithMicrophoneAsync(
                    serviceManager.CurrentSpeechConfig,
                    serviceManager.CurrentCluModel,
                    serviceManager.CurrentPatternMatchingModel,
                    taskCompletionSourceManager);
            speechRecognizer = new SpeechRecognizer(serviceManager.CurrentSpeechConfig);
        }
        protected override async void OnDisappearing()
        {
            base.OnDisappearing();

            if (speechSynthesizer != null)
            {
                await speechSynthesizer.StopSpeakingAsync();
                speechSynthesizer.Dispose();
            }

            if (intentRecognizerByPatternMatching != null)
            {
                await intentRecognizerByPatternMatching.StopContinuousRecognitionAsync();
                intentRecognizerByPatternMatching.Dispose();
            }

            if (intentRecognizerByCLU != null)
            {
                await intentRecognizerByCLU.StopContinuousRecognitionAsync();
                intentRecognizerByCLU.Dispose();
            }
        }
        
        private async void ContentPage_Loaded(object sender, EventArgs e)
        {
            await CheckAndRequestMicrophonePermission();
        }

        private async Task<PermissionStatus> CheckAndRequestMicrophonePermission()
        {
            PermissionStatus status = await Permissions.CheckStatusAsync<Permissions.Microphone>();
            if (status == PermissionStatus.Granted)
            {
                return status;
            }
            if (Permissions.ShouldShowRationale<Permissions.Microphone>())
            {
                // Prompt the user with additional information as to why the permission is needed
                await DisplayAlert("Permission required", "Microphone permission is necessary", "OK");
            }
            status = await Permissions.RequestAsync<Permissions.Microphone>();
            return status;
        }

        private static async Task ContinuousIntentPatternMatchingWithMicrophoneAsync(
            IntentRecognizer intentRecognizer, TaskCompletionSourceManager<int> stopRecognition)
        {
            await intentRecognizer.StartContinuousRecognitionAsync();
            // Waits for completion. Use Task.WaitAny to keep the task rooted.
            Task.WaitAny(new[] { stopRecognition.TaskCompletionSource.Task });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        /// <param name="cluModel"></param>
        /// <param name="patternMatchingModelCollection"></param>
        /// <param name="stopRecognitionManager"></param>
        /// <returns>una tupla contentente nell'ordine un intent recognizer basato su Patter Matching, un sintetizzatore vocale e un intent recognizer basato su un modello di Conversational Language Understanding </returns>
        private static (IntentRecognizer, SpeechSynthesizer, IntentRecognizer) ConfigureContinuousIntentPatternMatchingWithMicrophoneAsync(
            SpeechConfig config,
            ConversationalLanguageUnderstandingModel cluModel,
            LanguageUnderstandingModelCollection patternMatchingModelCollection,
            TaskCompletionSourceManager<int> stopRecognitionManager)
        {
            //creazione di un intent recognizer basato su pattern matching
            var intentRecognizerByPatternMatching = new IntentRecognizer(config);
            intentRecognizerByPatternMatching.ApplyLanguageModels(patternMatchingModelCollection);

            //creazione di un intent recognizer basato su CLU
            var intentRecognizerByCLU = new IntentRecognizer(config);
            var modelsCollection = new LanguageUnderstandingModelCollection { cluModel };
            intentRecognizerByCLU.ApplyLanguageModels(modelsCollection);

            //creazione di un sitetizzatore vocale
            var synthesizer = new SpeechSynthesizer(config);

            //gestione eventi
            intentRecognizerByPatternMatching.Recognized += async (s, e) =>
            {
                switch (e.Result.Reason)
                {
                    case ResultReason.RecognizedSpeech:
                        Debug.WriteLine($"PATTERN MATCHING - RECOGNIZED SPEECH: Text= {e.Result.Text}");
                        break;
                    case ResultReason.RecognizedIntent:
                        {
                            Debug.WriteLine($"PATTERN MATCHING - RECOGNIZED INTENT: Text= {e.Result.Text}");
                            Debug.WriteLine($"       Intent Id= {e.Result.IntentId}.");
                            if (e.Result.IntentId == "Ok")
                            {
                                Debug.WriteLine("Stopping current speaking if any...");
                                await synthesizer.StopSpeakingAsync();
                                Debug.WriteLine("Stopping current intent recognition by CLU if any...");
                                await intentRecognizerByCLU.StopContinuousRecognitionAsync();
                                await HandleOkCommand(synthesizer, intentRecognizerByCLU).ConfigureAwait(false);
                            }
                            else if (e.Result.IntentId == "Stop")
                            {
                                Debug.WriteLine("Stopping current speaking...");
                                await synthesizer.StopSpeakingAsync();
                            }
                        }

                        break;
                    case ResultReason.NoMatch:
                        Debug.WriteLine($"NOMATCH: Speech could not be recognized.");
                        var noMatch = NoMatchDetails.FromResult(e.Result);
                        switch (noMatch.Reason)
                        {
                            case NoMatchReason.NotRecognized:
                                Debug.WriteLine($"PATTERN MATCHING - NOMATCH: Speech was detected, but not recognized.");
                                break;
                            case NoMatchReason.InitialSilenceTimeout:
                                Debug.WriteLine($"PATTERN MATCHING - NOMATCH: The start of the audio stream contains only silence, and the service timed out waiting for speech.");
                                break;
                            case NoMatchReason.InitialBabbleTimeout:
                                Debug.WriteLine($"PATTERN MATCHING - NOMATCH: The start of the audio stream contains only noise, and the service timed out waiting for speech.");
                                break;
                            case NoMatchReason.KeywordNotRecognized:
                                Debug.WriteLine($"PATTERN MATCHING - NOMATCH: Keyword not recognized");
                                break;
                        }
                        break;

                    default:
                        break;
                }
            };
            intentRecognizerByPatternMatching.Canceled += (s, e) =>
            {
                Debug.WriteLine($"PATTERN MATCHING - CANCELED: Reason={e.Reason}");

                if (e.Reason == CancellationReason.Error)
                {
                    Debug.WriteLine($"PATTERN MATCHING - CANCELED: ErrorCode={e.ErrorCode}");
                    Debug.WriteLine($"PATTERN MATCHING - CANCELED: ErrorDetails={e.ErrorDetails}");
                    Debug.WriteLine($"PATTERN MATCHING - CANCELED: Did you update the speech key and location/region info?");
                }
                stopRecognitionManager.TaskCompletionSource.TrySetResult(0);
            };
            intentRecognizerByPatternMatching.SessionStopped += (s, e) =>
            {
                Debug.WriteLine("\n    Session stopped event.");
                stopRecognitionManager.TaskCompletionSource.TrySetResult(0);
            };

            return (intentRecognizerByPatternMatching, synthesizer, intentRecognizerByCLU);

        }
        private static async Task HandleOkCommand(SpeechSynthesizer synthesizer, IntentRecognizer intentRecognizer)
        {
            await synthesizer.SpeakTextAsync("Sono in ascolto");
            //avvia l'intent recognition su Azure
            string? jsonResult = await RecognizeIntentAsync(intentRecognizer);
            if (jsonResult != null)
            {
                //process jsonResult
                //deserializzo il json
                CLUResponse cluResponse = JsonSerializer.Deserialize<CLUResponse>(jsonResult, jsonSerializationOptions) ?? new CLUResponse();
                await synthesizer.SpeakTextAsync($"La tua richiesta è stata {cluResponse.Result?.Query}");
                var topIntent = cluResponse.Result?.Prediction?.TopIntent;

                if (topIntent != null)
                {
                    switch (topIntent)
                    {
                        case string intent when intent.Contains("Weather"):
                            await synthesizer.SpeakTextAsync("Vuoi sapere come è il tempo");
                            
                            string? data = cluResponse.Result?.Prediction?.Entities?[1].Text;

                            string? città = cluResponse.Result?.Prediction?.Entities?[2].Text;
                           
                            await PrevisioniMeteo(città, data, synthesizer);
                            break;
                        case string intent when intent.Contains("Places") && intent != "Places.GetDistance":
                            await synthesizer.SpeakTextAsync("Vuoi informazioni geolocalizzate");
                            string? placeData = cluResponse.Result?.Prediction?.Entities?[0].Text;
                            string? placeCitta = cluResponse.Result?.Prediction?.Entities?[1].Text;

                            string traduzione = Traduzione.Traduci(placeData);
                            await CercaPOI(placeCitta, traduzione, synthesizer);

                            break;
                        case string intent when intent.Contains("Places.GetDistance"):
                            await synthesizer.SpeakTextAsync("Trova Distanza");
                            string? partenza = cluResponse.Result?.Prediction?.Entities?[0].Text;
                            string? arrivo = cluResponse.Result?.Prediction?.Entities?[1].Text;

                            await CalcolaPercorso(partenza, arrivo, synthesizer);
                            break;
                        case string intent when intent.Contains("None"):
                            await synthesizer.SpeakTextAsync("Non ho capito");
                            break;
                    }

                }
                //determino l'action da fare, eventualmente effettuando una richiesta GET su un endpoint remoto scelto in base al topScoringIntent
                //ottengo il risultato dall'endpoit remoto
                //effettuo un text to speech per descrivere il risultato
            }
            else
            {
                //è stato restituito null - ad esempio quando il processo è interrotto prima di ottenre la risposta dal server
                Debug.WriteLine("Non è stato restituito nulla dall'intent reconition sul server");
            }
        }

        public static async Task<string?> RecognizeIntentAsync(IntentRecognizer recognizer)
        {
            // Starts recognizing.
            Debug.WriteLine("Say something...");

            // Starts intent recognition, and returns after a single utterance is recognized. The end of a
            // single utterance is determined by listening for silence at the end or until a maximum of 15
            // seconds of audio is processed.  The task returns the recognition text as result. 
            // Note: Since RecognizeOnceAsync() returns only a single utterance, it is suitable only for single
            // shot recognition like command or query. 
            // For long-running multi-utterance recognition, use StartContinuousRecognitionAsync() instead.
            var result = await recognizer.RecognizeOnceAsync();
            string? languageUnderstandingJSON = null;

            // Checks result.
            switch (result.Reason)
            {
                case ResultReason.RecognizedIntent:
                    Debug.WriteLine($"RECOGNIZED: Text={result.Text}");
                    Debug.WriteLine($"    Intent Id: {result.IntentId}.");
                    languageUnderstandingJSON = result.Properties.GetProperty(PropertyId.LanguageUnderstandingServiceResponse_JsonResult);
                    Debug.WriteLine($"    Language Understanding JSON: {languageUnderstandingJSON}.");
                    CLUResponse cluResponse = JsonSerializer.Deserialize<CLUResponse>(languageUnderstandingJSON, jsonSerializationOptions) ?? new CLUResponse();
                    Debug.WriteLine("Risultato deserializzato:");
                    Debug.WriteLine($"kind: {cluResponse.Kind}");
                    Debug.WriteLine($"result.query: {cluResponse.Result?.Query}");
                    Debug.WriteLine($"result.prediction.topIntent: {cluResponse.Result?.Prediction?.TopIntent}");
                    Debug.WriteLine($"result.prediction.Intents[0].Category: {cluResponse.Result?.Prediction?.Intents?[0].Category}");
                    Debug.WriteLine($"result.prediction.Intents[0].ConfidenceScore: {cluResponse.Result?.Prediction?.Intents?[0].ConfidenceScore}");
                    Debug.WriteLine($"result.prediction.entities: ");
                    cluResponse.Result?.Prediction?.Entities?.ForEach(s => Debug.WriteLine($"\tcategory = {s.Category}; text= {s.Text};"));
                    break;
                case ResultReason.RecognizedSpeech:
                    Debug.WriteLine($"RECOGNIZED: Text={result.Text}");
                    Debug.WriteLine($"    Intent not recognized.");
                    break;
                case ResultReason.NoMatch:
                    Debug.WriteLine($"NOMATCH: Speech could not be recognized.");
                    break;
                case ResultReason.Canceled:
                    var cancellation = CancellationDetails.FromResult(result);
                    Debug.WriteLine($"CANCELED: Reason={cancellation.Reason}");

                    if (cancellation.Reason == CancellationReason.Error)
                    {
                        Debug.WriteLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                        Debug.WriteLine($"CANCELED: ErrorDetails={cancellation.ErrorDetails}");
                        Debug.WriteLine($"CANCELED: Did you update the subscription info?");
                    }
                    break;
            }
            return languageUnderstandingJSON;
        }
        private async void OnRecognitionButtonClicked2(object sender, EventArgs e)
        {
            if(serviceManager != null && taskCompletionSourceManager != null)
            {
                buttonToggle = !buttonToggle;
                if (buttonToggle)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        buttonToggleColor = RecognizeSpeechBtn.Background;
                    });

                    RecognizeSpeechBtn.Background = Colors.Yellow;
                    //creo le risorse
                    //su un dispositivo mobile potrebbe succedere che cambiando rete cambino i parametri della rete, ed in particolare il proxy
                    //In questo caso, per evitare controlli troppo complessi, si è scelto di ricreare lo speechConfig ad ogni richiesta se cambia il proxy
                    if (serviceManager.ShouldRecreateSpeechConfigForProxyChange())
                    {
                        (intentRecognizerByPatternMatching, speechSynthesizer, intentRecognizerByCLU) =
                       ConfigureContinuousIntentPatternMatchingWithMicrophoneAsync(
                           serviceManager.CurrentSpeechConfig,
                           serviceManager.CurrentCluModel,
                           serviceManager.CurrentPatternMatchingModel,
                           taskCompletionSourceManager);
                    }

                    _ = Task.Factory.StartNew(async () =>
                    {
                        taskCompletionSourceManager.TaskCompletionSource = new TaskCompletionSource<int>();
                        await ContinuousIntentPatternMatchingWithMicrophoneAsync(
                            intentRecognizerByPatternMatching!, taskCompletionSourceManager)
                        .ConfigureAwait(false);
                    });
                }
                else
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        RecognizeSpeechBtn.Background = buttonToggleColor;
                    });
                    //la doppia chiamata di StopSpeakingAsync è un work-around a un problema riscontrato in alcune situazioni:
                    //se si prova a fermare il task mentre il sintetizzatore sta parlando, in alcuni casi si verifica un'eccezione. 
                    //Con il doppio StopSpeakingAsync non succede.
                    await speechSynthesizer!.StopSpeakingAsync();
                    await speechSynthesizer.StopSpeakingAsync();
                    await intentRecognizerByCLU!.StopContinuousRecognitionAsync();
                    await intentRecognizerByPatternMatching!.StopContinuousRecognitionAsync();
                    //speechSynthesizer.Dispose();
                    //intentRecognizerByPatternMatching.Dispose();
                }
            }
        }
        private async void OnRecognitionButtonClicked(object sender, EventArgs e)
        {
            try
            {
                //accedo ai servizi
                //AzureCognitiveServicesResourceManager serviceManager =(Application.Current as App).AzureCognitiveServicesResourceManager;
                // Creates a speech recognizer using microphone as audio input.
                // Starts speech recognition, and returns after a single utterance is recognized. The end of a
                // single utterance is determined by listening for silence at the end or until a maximum of 15
                // seconds of audio is processed.  The task returns the recognition text as result.
                // Note: Since RecognizeOnceAsync() returns only a single utterance, it is suitable only for single
                // shot recognition like command or query.
                // For long-running multi-utterance recognition, use StartContinuousRecognitionAsync() instead.
                var result = await speechRecognizer!.RecognizeOnceAsync().ConfigureAwait(false);

                // Checks result.
                StringBuilder sb = new();
                if (result.Reason == ResultReason.RecognizedSpeech)
                {
                    sb.AppendLine($"RECOGNIZED: Text={result.Text}");
                    await speechSynthesizer!.SpeakTextAsync(result.Text);
                }
                else if (result.Reason == ResultReason.NoMatch)
                {
                    sb.AppendLine($"NOMATCH: Speech could not be recognized.");
                }
                else if (result.Reason == ResultReason.Canceled)
                {
                    var cancellation = CancellationDetails.FromResult(result);
                    sb.AppendLine($"CANCELED: Reason={cancellation.Reason}");

                    if (cancellation.Reason == CancellationReason.Error)
                    {
                        sb.AppendLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                        sb.AppendLine($"CANCELED: ErrorDetails={cancellation.ErrorDetails}");
                        sb.AppendLine($"CANCELED: Did you update the subscription info?");
                    }
                }
                UpdateUI(sb.ToString());
            }
            catch (Exception ex)
            {
                UpdateUI("Exception: " + ex.ToString());
            }
        }
        private void UpdateUI(String message)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                RecognitionText.Text = message;
            });
        }
        public static async Task<(double? lat, double? lon)?>
        GetCoordinate(string? città, string language = "it", int count = 1)
        {
            string? cittaCod = HttpUtility.UrlEncode(città);
            string urlCoordinate = $"https://geocoding-api.open-meteo.com/v1/search?name={cittaCod}&count={count}&language={language}";
            try
            {
                HttpResponseMessage response = await _client.GetAsync($"{urlCoordinate}");
                if (response.IsSuccessStatusCode)
                {
                    //await Console.Out.WriteLineAsync(await response.Content.ReadAsStringAsync());
                    GeoCoding? geoCoding = await response.Content.ReadFromJsonAsync<GeoCoding>();
                    if (geoCoding != null && geoCoding.Results?.Count > 0)
                    {
                        return (geoCoding.Results[0].Latitude, geoCoding.Results[0].Longitude);
                    }
                }
                return null;
            }
            catch (Exception)
            {

                Console.WriteLine("Errore");
            }
            return null;
        }
        public static async Task CalcolaPercorso(string partenza, string arrivo, SpeechSynthesizer synthesizer)
        {
            FormattableString addressUrl = $"https://dev.virtualearth.net/REST/v1/Routes/Driving?wayPoint.0={partenza}&wayPoint.1={arrivo}&maxSolutions=1&key=ApFevOLbYr3EA5LKRRkSO_YWjg6D-432NQK6w7cSlVXJM08dtslT5fR5Y0M-dXoX";
            string url = FormattableString.Invariant(addressUrl);
            var response = await _client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var route = await response.Content.ReadFromJsonAsync<RouteCalculator>();
                if (route != null)
                {
                    await synthesizer.SpeakTextAsync($"La distanza è di {route.ResourceSets[0].Resources[0].TravelDistance} chilometri e" +
                        $" la durata è di {(route.ResourceSets[0].Resources[0].TravelDuration) / 60} minuti");
                }
            }
        }
        public static async Task CercaPOI(string citta, string data, SpeechSynthesizer synthesizer)
        {
            var geo = await GetCoordinate(citta);
            if (geo != null)
            {
                FormattableString addressUrl = $"https://dev.virtualearth.net/REST/v1/LocationRecog/{geo.Value.lat},{geo.Value.lon}?type={data}&radius=1&verboseplacenames=true&key=ApFevOLbYr3EA5LKRRkSO_YWjg6D-432NQK6w7cSlVXJM08dtslT5fR5Y0M-dXoX";
                string url = FormattableString.Invariant(addressUrl);
                var response = await _client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var POI = await response.Content.ReadFromJsonAsync<PointOfInterest>();
                    if (POI != null)
                    {
                        await synthesizer.SpeakTextAsync($"Il POI più vicino è {POI.ResourceSets[0].Resources[0].BusinessesAtLocation[0].BusinessInfo.EntityName}");
                    }
                }
            }
        }
        public static async Task PrevisioniMeteo(string città, string data, SpeechSynthesizer synthesizer)
        {
            const string datoNonFornitoString = "";
           
            var geo = await GetCoordinate(città);
            if (geo != null)
            {
                Console.WriteLine(geo.Value.lat + " " + geo.Value.lon);
                FormattableString addressUrlFormattable = $"https://api.open-meteo.com/v1/forecast?latitude={geo?.lat}&longitude={geo?.lon}&current=temperature_2m,weather_code,wind_speed_10m,wind_direction_10m&hourly=temperature_2m,relative_humidity_2m,dew_point_2m,apparent_temperature,precipitation_probability,precipitation,rain,showers,weather_code,wind_speed_10m,wind_direction_10m&daily=weather_code,temperature_2m_max,temperature_2m_min,apparent_temperature_max,apparent_temperature_min&timeformat=unixtime&timezone=auto";
                Console.WriteLine(addressUrlFormattable);
                string addressUrl = FormattableString.Invariant(addressUrlFormattable);
                var response = await _client.GetAsync($"{addressUrl}");
                if (response.IsSuccessStatusCode)
                {
                    OpenMeteoForecast? forecast = await response.Content.ReadFromJsonAsync<OpenMeteoForecast>();
                    if (forecast != null)
                    {
                        await synthesizer.SpeakTextAsync($"\nCondizioni meteo attuali per {città}");
                        await synthesizer.SpeakTextAsync($"Data e ora previsione: {Utils1.Display(Utils1.UnixTimeStampToDateTime(forecast.Current.Time), datoNonFornitoString)}");
                        await synthesizer.SpeakTextAsync($"Temperatura : {Utils1.Display(forecast.Current.Temperature2m, datoNonFornitoString)} °C");
                        await synthesizer.SpeakTextAsync($"previsione: {Utils1.Display(Utils1.WMOCodesIntIT(forecast.Current.WeatherCode), datoNonFornitoString)}");
                        await synthesizer.SpeakTextAsync($"Direzione del vento: {Utils1.Display(forecast.Current.WindDirection10m, datoNonFornitoString)} °");
                        await synthesizer.SpeakTextAsync($"Velocità del vento: {Utils1.Display(forecast.Current.WindSpeed10m, datoNonFornitoString)} Km/h");

                    }
                    if (forecast.Daily != null)
                    {
                        Console.WriteLine($"\nPrevisioni meteo giornaliere per {città}");
                        int? numeroGiorni = forecast.Daily.Time?.Count;
                        if (numeroGiorni > 0)
                        {
                            for (int i = 0; i < numeroGiorni; i++)
                            {
                                await synthesizer.SpeakTextAsync($"Data e ora = {Utils1.Display(Utils1.UnixTimeStampToDateTime(forecast.Daily?.Time?[i]), datoNonFornitoString)};" +
                                    $" T max = {Utils1.Display(forecast.Daily?.Temperature2mMax?[i], datoNonFornitoString)} °C;" +
                                    $" T min = {Utils1.Display(forecast.Daily?.Temperature2mMin?[i], datoNonFornitoString)} °C; " +
                                    $"previsione = {Utils1.Display(Utils1.WMOCodesIntIT(forecast.Daily?.WeatherCode?[i]), datoNonFornitoString)}");
                            }
                        }
                    }
                    if (forecast.Hourly != null)
                    {
                        await synthesizer.SpeakTextAsync($"\nPrevisioni meteo ora per ora per {città}");
                        int? numeroPrevisioni = forecast.Hourly.Time?.Count;
                        if (numeroPrevisioni > 0)
                        {
                            for (int i = 0; i < numeroPrevisioni; i++)
                            {
                                await synthesizer.SpeakTextAsync($"Data e ora = {Utils1.Display(Utils1.UnixTimeStampToDateTime(forecast.Hourly.Time?[i]), datoNonFornitoString)};" +
                                    $" T = {Utils1.Display(forecast.Hourly.Temperature2m?[i], datoNonFornitoString)} °C;" +
                                    $" U = {Utils1.Display(forecast.Hourly.RelativeHumidity2m?[i], datoNonFornitoString)} %;" +
                                    $" T percepita = {Utils1.Display(forecast.Hourly.ApparentTemperature?[i], datoNonFornitoString)};" +
                                    $" Prob. di rovesci = {Utils1.Display(forecast.Hourly.PrecipitationProbability?[i] * 1.0, datoNonFornitoString)} %; " +
                                    $" previsione = {Utils1.Display(Utils1.WMOCodesIntIT(forecast.Hourly.WeatherCode?[i]), datoNonFornitoString)}");
                            }
                        }
                    }

                }
            }
        }
    }
}
