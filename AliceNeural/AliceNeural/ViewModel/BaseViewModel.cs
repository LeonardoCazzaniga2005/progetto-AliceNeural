using AliceNeural.Helper;
using AliceNeural.Models;
using AliceNeural.Utils;
using AliceNeural.View;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HttpProxyControl;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Intent;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace AliceNeural.ViewModel
{
    public partial class BaseViewModel : ObservableObject
    {
        #region DichiarazioneVariabili
        static readonly HttpClient _client = HttpProxyHelper.CreateHttpClient(setProxy: true);
        SpeechRecognizer? speechRecognizer;
        IntentRecognizer? intentRecognizerByPatternMatching;
        IntentRecognizer? intentRecognizerByCLU;
        SpeechSynthesizer? speechSynthesizer;
        TaskCompletionSourceManager<int>? taskCompletionSourceManager;
        AzureCognitiveServicesResourceManager? serviceManager;
        [ObservableProperty]
        double opNeve = 0.1;
        [ObservableProperty]
        double opSole = 0.1;
        [ObservableProperty]
        double opNuvole = 0.1;
        [ObservableProperty]
        double opPioggia = 0.1;
        [ObservableProperty]
        Color coloreBottone = Colors.Red;
        [ObservableProperty]
        string dataPrevisioni;
        [ObservableProperty]
        string citta;
        [ObservableProperty]
        bool meteoVisible = false;
        [ObservableProperty]
        bool mapVisible = false;
        [ObservableProperty]
        bool mapDistance = false;
        [ObservableProperty]
        bool distVisible = false;
        [ObservableProperty]
        string tipo;
        [ObservableProperty]
        string nomePosto;
        [ObservableProperty]
        string cittaPartenza;
        [ObservableProperty]
        string cittaArrivo;
        [ObservableProperty]
        string distanza;
        [ObservableProperty]
        string durata;
        #endregion
        private static readonly JsonSerializerOptions? jsonSerializationOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
        public BaseViewModel()
        {
            new Action(async () =>
            {
                await Init();
            })();
        }
        public async Task Init()
        {
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
        #region richieste
        public async Task CercaPOI(string citta, string data, SpeechSynthesizer synthesizer)
        {
            var geo = await GetCoordinate(citta);
            FormattableString addressUrl = $"https://dev.virtualearth.net/REST/v1/LocationRecog/{geo.Value.lat},{geo.Value.lon}?type={data}&radius=1&verboseplacenames=true&key=ApFevOLbYr3EA5LKRRkSO_YWjg6D-432NQK6w7cSlVXJM08dtslT5fR5Y0M-dXoX";
            string url = FormattableString.Invariant(addressUrl);
            var response = await _client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var POI = await response.Content.ReadFromJsonAsync<PointOfInterest>();
                if (POI != null)
                {
                    NomePosto = POI.ResourceSets[0].Resources[0].BusinessesAtLocation[0].BusinessInfo.EntityName;
                    await synthesizer.SpeakTextAsync($"Il {data} più vicino è {POI.ResourceSets[0].Resources[0].BusinessesAtLocation[0].BusinessInfo.EntityName}");
                }
            }
        }
        public  async Task CercaPoiConCoord(Location location, string data, SpeechSynthesizer synthesizer)
        {
            FormattableString addressUrl = $"https://dev.virtualearth.net/REST/v1/LocationRecog/{location.Latitude},{location.Longitude}?type={data}&radius=1&verboseplacenames=true&key=ApFevOLbYr3EA5LKRRkSO_YWjg6D-432NQK6w7cSlVXJM08dtslT5fR5Y0M-dXoX";
            string url = FormattableString.Invariant(addressUrl);
            var response = await _client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var POI = await response.Content.ReadFromJsonAsync<PointOfInterest>();
                if (POI != null)
                {
                    NomePosto = POI.ResourceSets[0].Resources[0].BusinessesAtLocation[0].BusinessInfo.EntityName;
                    await synthesizer.SpeakTextAsync($"Il {data} più vicino è {POI.ResourceSets[0].Resources[0].BusinessesAtLocation[0].BusinessInfo.EntityName}");
                }
            }
        }
        public  async Task CercaPoiConNomeECoord(Location location, string name, SpeechSynthesizer synthesizer)
        {
            FormattableString addressUrl = $"https://dev.virtualearth.net/REST/v1/LocationRecog/{location.Latitude},{location.Longitude}?q={name}&key=ApFevOLbYr3EA5LKRRkSO_YWjg6D-432NQK6w7cSlVXJM08dtslT5fR5Y0M-dXoX";
            string url = FormattableString.Invariant(addressUrl);
            var response = await _client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var POI = await response.Content.ReadFromJsonAsync<PointOfInterest>();
                if (POI != null)
                {
                    NomePosto = POI.ResourceSets[0].Resources[0].BusinessesAtLocation[0].BusinessInfo.EntityName;
                    await synthesizer.SpeakTextAsync($"Il {name} più vicino è {POI.ResourceSets[0].Resources[0].BusinessesAtLocation[0].BusinessAddress.FormattedAddress}");
                }
            }
        }
        public  async Task CercaPoiConNome(string citta, string name, SpeechSynthesizer synthesizer)
        {
            var geo = await GetCoordinate(citta);
            FormattableString addressUrl = $"https://dev.virtualearth.net/REST/v1/LocationRecog/{geo.Value.lat},{geo.Value.lon}?q={name}&key=ApFevOLbYr3EA5LKRRkSO_YWjg6D-432NQK6w7cSlVXJM08dtslT5fR5Y0M-dXoX";
            string url = FormattableString.Invariant(addressUrl);
            var response = await _client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var POI = await response.Content.ReadFromJsonAsync<PointOfInterest>();
                if (POI != null)
                {
                    NomePosto = POI.ResourceSets[0].Resources[0].BusinessesAtLocation[0].BusinessInfo.EntityName;
                    await synthesizer.SpeakTextAsync($"Il {name} più vicino è {POI.ResourceSets[0].Resources[0].BusinessesAtLocation[0].BusinessAddress.FormattedAddress}");
                }
            }
        }
        public  async Task PrevisioniMeteo(string città, SpeechSynthesizer synthesizer)
        {
            const string datoNonFornitoString = "";

            var geo = await GetCoordinate(città);
            if (geo != null)
            {
                FormattableString addressUrlFormattable = $"https://api.open-meteo.com/v1/forecast?latitude={geo?.lat}&longitude={geo?.lon}&current=temperature_2m,weather_code,wind_speed_10m,wind_direction_10m&hourly=temperature_2m,relative_humidity_2m,dew_point_2m,apparent_temperature,precipitation_probability,precipitation,rain,showers,weather_code,wind_speed_10m,wind_direction_10m&daily=weather_code,temperature_2m_max,temperature_2m_min,apparent_temperature_max,apparent_temperature_min&timeformat=unixtime&timezone=auto";
                string addressUrl = FormattableString.Invariant(addressUrlFormattable);
                var response = await _client.GetAsync($"{addressUrl}");
                if (response.IsSuccessStatusCode)
                {
                    OpenMeteoForecast? forecast = await response.Content.ReadFromJsonAsync<OpenMeteoForecast>();
                    string meteo = ConvertWeather.ConvertWeatherCode(forecast.Current.WeatherCode);
                    Citta = città;
                    DataPrevisioni = DateTime.Today.ToShortDateString();
                    ConvertOpacity(meteo);
                    if (forecast != null)
                    {
                        await synthesizer.SpeakTextAsync($"Temperatura : {Utils1.Display(forecast.Current.Temperature2m, datoNonFornitoString)} gradi");
                        await synthesizer.SpeakTextAsync($"Previsioni: {Utils1.Display(Utils1.WMOCodesIntIT(forecast.Current.WeatherCode), datoNonFornitoString)}");
                        await synthesizer.SpeakTextAsync($"Direzione del vento: {Utils1.Display(forecast.Current.WindDirection10m, datoNonFornitoString)} gradi");
                        await synthesizer.SpeakTextAsync($"Velocità del vento: {Utils1.Display(forecast.Current.WindSpeed10m, datoNonFornitoString)} chilometri orari");

                    }
                    
                }
            }
        }
        public  async Task PrevisioniMeteoConCoord(Location location, SpeechSynthesizer synthesizer)
        {
            const string datoNonFornitoString = "";
            FormattableString addressUrlFormattable = $"https://api.open-meteo.com/v1/forecast?latitude={location.Latitude}&longitude={location.Longitude}&current=temperature_2m,weather_code,wind_speed_10m,wind_direction_10m&hourly=temperature_2m,relative_humidity_2m,dew_point_2m,apparent_temperature,precipitation_probability,precipitation,rain,showers,weather_code,wind_speed_10m,wind_direction_10m&daily=weather_code,temperature_2m_max,temperature_2m_min,apparent_temperature_max,apparent_temperature_min&timeformat=unixtime&timezone=auto";
            string addressUrl = FormattableString.Invariant(addressUrlFormattable);
            var response = await _client.GetAsync($"{addressUrl}");
            if (response.IsSuccessStatusCode)
            {
                OpenMeteoForecast? forecast = await response.Content.ReadFromJsonAsync<OpenMeteoForecast>();
                string meteo = ConvertWeather.ConvertWeatherCode(forecast.Current.WeatherCode);
                ConvertOpacity(meteo);
                DataPrevisioni = DateTime.Today.ToShortDateString();
                Citta = await ReverseGeoCoding(location.Latitude, location.Longitude);
                if (forecast != null)
                {
                    await synthesizer.SpeakTextAsync($"Temperatura : {Utils1.Display(forecast.Current.Temperature2m, datoNonFornitoString)} gradi");
                    await synthesizer.SpeakTextAsync($"previsione: {Utils1.Display(Utils1.WMOCodesIntIT(forecast.Current.WeatherCode), datoNonFornitoString)}");
                    await synthesizer.SpeakTextAsync($"Direzione del vento: {Utils1.Display(forecast.Current.WindDirection10m, datoNonFornitoString)} gradi");
                    await synthesizer.SpeakTextAsync($"Velocità del vento: {Utils1.Display(forecast.Current.WindSpeed10m, datoNonFornitoString)} chilometri orari");

                }
            }
        }
        public  async Task<OpenMeteoForecast> PrevisioniMeteoCheck(Location location, SpeechSynthesizer synthesizer)
        {
            const string datoNonFornitoString = "";
            FormattableString addressUrlFormattable = $"https://api.open-meteo.com/v1/forecast?latitude={location.Latitude}&longitude={location.Longitude}&current=temperature_2m,weather_code,wind_speed_10m,wind_direction_10m&hourly=temperature_2m,relative_humidity_2m,dew_point_2m,apparent_temperature,precipitation_probability,precipitation,rain,showers,weather_code,wind_speed_10m,wind_direction_10m&daily=weather_code,temperature_2m_max,temperature_2m_min,apparent_temperature_max,apparent_temperature_min&timeformat=unixtime&timezone=auto";
            string addressUrl = FormattableString.Invariant(addressUrlFormattable);
            var response = await _client.GetAsync($"{addressUrl}");
            if (response.IsSuccessStatusCode)
            {
                OpenMeteoForecast? forecast = await response.Content.ReadFromJsonAsync<OpenMeteoForecast>();
                Citta = await ReverseGeoCoding(location.Latitude, location.Longitude);
                return forecast;
            }
            return null;
        }
        public  async Task<OpenMeteoForecast> PrevisioniMeteoCheckConCitta(string citta, SpeechSynthesizer synthesizer)
        {
            const string datoNonFornitoString = "";

            var geo = await GetCoordinate(citta);
            if (geo != null)
            {
                FormattableString addressUrlFormattable = $"https://api.open-meteo.com/v1/forecast?latitude={geo?.lat}&longitude={geo?.lon}&current=temperature_2m,weather_code,wind_speed_10m,wind_direction_10m&hourly=temperature_2m,relative_humidity_2m,dew_point_2m,apparent_temperature,precipitation_probability,precipitation,rain,showers,weather_code,wind_speed_10m,wind_direction_10m&daily=weather_code,temperature_2m_max,temperature_2m_min,apparent_temperature_max,apparent_temperature_min&timeformat=unixtime&timezone=auto";
                string addressUrl = FormattableString.Invariant(addressUrlFormattable);
                var response = await _client.GetAsync($"{addressUrl}");
                if (response.IsSuccessStatusCode)
                {
                    OpenMeteoForecast? forecast = await response.Content.ReadFromJsonAsync<OpenMeteoForecast>();
                    return forecast;
                }
            }
            return null;
        }
        public  async Task CalcolaPercorso(string partenza, string arrivo, SpeechSynthesizer synthesizer)
        {
            FormattableString addressUrl = $"https://dev.virtualearth.net/REST/v1/Routes/Driving?wayPoint.0={partenza}&wayPoint.1={arrivo}&maxSolutions=1&key=ApFevOLbYr3EA5LKRRkSO_YWjg6D-432NQK6w7cSlVXJM08dtslT5fR5Y0M-dXoX";
            string url = FormattableString.Invariant(addressUrl);
            var response = await _client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var route = await response.Content.ReadFromJsonAsync<RouteCalculator>();
                if (route != null)
                {
                    Distanza = $"{route.ResourceSets[0].Resources[0].TravelDistance} km";
                    Durata = $"{route.ResourceSets[0].Resources[0].TravelDuration / 60} minuti";
                    await synthesizer.SpeakTextAsync($"La distanza è di {route.ResourceSets[0].Resources[0].TravelDistance} chilometri e" +
                        $" la durata è di {(route.ResourceSets[0].Resources[0].TravelDuration) / 60} minuti");
                }
            }
        }
        #endregion
        #region microfono
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
                await Shell.Current.DisplayAlert("Permission required", "Microphone permission is necessary", "OK");
            }
            status = await Permissions.RequestAsync<Permissions.Microphone>();
            return status;
        }

        private  async Task ContinuousIntentPatternMatchingWithMicrophoneAsync(
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
        private  (IntentRecognizer, SpeechSynthesizer, IntentRecognizer) ConfigureContinuousIntentPatternMatchingWithMicrophoneAsync(
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
                        break;
                    case ResultReason.RecognizedIntent:
                        {
                            if (e.Result.IntentId == "Ok")
                            {
                                await synthesizer.StopSpeakingAsync();
                                await intentRecognizerByCLU.StopContinuousRecognitionAsync();
                                await HandleOkCommand(synthesizer, intentRecognizerByCLU).ConfigureAwait(false);
                            }
                            else if (e.Result.IntentId == "Stop")
                            {
                                await synthesizer.StopSpeakingAsync();
                            }
                        }

                        break;
                    case ResultReason.NoMatch:
                        var noMatch = NoMatchDetails.FromResult(e.Result);
                        break;

                    default:
                        break;
                }
            };
            intentRecognizerByPatternMatching.Canceled += (s, e) =>
            {
                stopRecognitionManager.TaskCompletionSource.TrySetResult(0);
            };
            intentRecognizerByPatternMatching.SessionStopped += (s, e) =>
            {
                stopRecognitionManager.TaskCompletionSource.TrySetResult(0);
            };

            return (intentRecognizerByPatternMatching, synthesizer, intentRecognizerByCLU);

        }
        #endregion
        private async Task HandleOkCommand(SpeechSynthesizer synthesizer, IntentRecognizer intentRecognizer)
        {
            await synthesizer.SpeakTextAsync("Sono in ascolto");
            //avvia l'intent recognition su Azure
            string? jsonResult = await RecognizeIntentAsync(intentRecognizer);
            if (jsonResult != null)
            {
                //process jsonResult
                //deserializzo il json
                CLUResponse cluResponse = JsonSerializer.Deserialize<CLUResponse>(jsonResult, jsonSerializationOptions) ?? new CLUResponse();
                var topIntent = cluResponse.Result?.Prediction?.TopIntent;

                if (topIntent != null)
                {
                    switch (topIntent)
                    {
                        case string intent when intent.Contains("CheckWeatherValue"):
                            try
                            {
                                MeteoVisible = true;
                                MapVisible = false;
                                MapDistance = false;
                                DistVisible = false;
                                string? citta = cluResponse.Result?.Prediction?.Entities?.Where(x => x.Category.ToLower().Contains("absolutelocation") || x.Category.ToLower().Contains("placename")).FirstOrDefault()?.Text;
                                if (citta != null)
                                {
                                    await synthesizer.SpeakTextAsync($"Recupero informazioni meteo a {citta}");
                                    await PrevisioniMeteo(citta, synthesizer);
                                }
                                else
                                {
                                    var location = await Geolocation.GetLastKnownLocationAsync();
                                    await synthesizer.SpeakTextAsync("Recupero informazioni meteo nella tua posizione");
                                    await PrevisioniMeteoConCoord(location, synthesizer);
                                }
                            }
                            catch(Exception ex)
                            {
                                await synthesizer.SpeakTextAsync("Errore nel recupero dei dati");
                                break;
                            }
                            break;
                        case string intent when intent.Contains("CheckWeatherTime"):
                            try
                            {
                                MeteoVisible = true;
                                MapVisible = false;
                                MapDistance = false;
                                DistVisible = false;
                                string? condizioni = cluResponse.Result?.Prediction?.Entities?.Where(x => x.Category.ToLower().Contains("weathercondition")).FirstOrDefault()?.Text;
                                string? citta = cluResponse.Result?.Prediction?.Entities?.Where(x => x.Category.ToLower().Contains("absolutelocation") || x.Category.ToLower().Contains("placename")).FirstOrDefault()?.Text;
                                if (citta is null)
                                {
                                    var location = await Geolocation.GetLastKnownLocationAsync();
                                    await synthesizer.SpeakTextAsync($"Recupero informazioni meteo");
                                    var meteo = await PrevisioniMeteoCheck(location, synthesizer);
                                    int? data = 0;
                                    switch (condizioni)
                                    {
                                        case string cond when cond.ToLower().Contains("pio"):
                                            data = meteo?.Daily?.WeatherCode?.Where(x => (x >= 50 && x <= 67) || (x >= 80 && x <= 82) || (x >= 95 && x <= 99)).FirstOrDefault();
                                            break;
                                        case string cond when cond.ToLower().Contains("sole"):
                                            data = meteo?.Daily?.WeatherCode?.Where(x => x == 0 || x==1).FirstOrDefault();
                                            break;
                                        case string cond when cond.ToLower().Contains("nuvo"):
                                            data = meteo?.Daily?.WeatherCode?.Where(x => x == 2 || x == 3).FirstOrDefault();
                                            break;
                                        case string cond when cond.ToLower().Contains("nev"):
                                            data = meteo?.Daily?.WeatherCode?.Where(x => (x>=71 && x<=77) || (x==85 || x==86)).FirstOrDefault();
                                            break;
                                    }
                                    if (data > 0)
                                    {
                                        string condizione = ConvertWeather.ConvertWeatherCode(data);
                                        ConvertOpacity(condizione);
                                        DateTime dataPioggia = DateTime.Today.AddDays(meteo.Daily.WeatherCode.IndexOf(data));
                                        DataPrevisioni = dataPioggia.ToShortDateString();
                                        await synthesizer.SpeakTextAsync($"La condizione richiesta ci sarà il {dataPioggia.ToShortDateString()}");
                                    }
                                    else
                                    {
                                        ConvertOpacity("null");
                                        DataPrevisioni = null;
                                        await synthesizer.SpeakTextAsync("La condizione richiesta non avverrà nei prossimi giorni");
                                    }
                                }
                                else
                                {
                                    await synthesizer.SpeakTextAsync($"Recupero informazioni meteo a {citta}");
                                    var meteo = await PrevisioniMeteoCheckConCitta(citta, synthesizer);
                                    int? data = 0;
                                    switch (condizioni)
                                    {
                                        case string cond when cond.ToLower().Contains("pio"):
                                            data = meteo?.Daily?.WeatherCode?.Where(x => (x >= 50 && x <= 67) || (x >= 80 && x <= 82) || (x >= 95 && x <= 99)).FirstOrDefault();
                                            break;
                                        case string cond when cond.ToLower().Contains("sole"):
                                            data = meteo?.Daily?.WeatherCode?.Where(x => x == 0 || x == 1).FirstOrDefault();
                                            break;
                                        case string cond when cond.ToLower().Contains("nuvo"):
                                            data = meteo?.Daily?.WeatherCode?.Where(x => x == 2 || x == 3).FirstOrDefault();
                                            break;
                                        case string cond when cond.ToLower().Contains("nev"):
                                            data = meteo?.Daily?.WeatherCode?.Where(x => (x >= 71 && x <= 77) || (x == 85 || x == 86)).FirstOrDefault();
                                            break;
                                    }
                                    if (data > 0)
                                    {
                                        string condizione = ConvertWeather.ConvertWeatherCode(data);
                                        ConvertOpacity(condizione);
                                        Citta = citta;
                                        DateTime dataPioggia = DateTime.Today.AddDays(meteo.Daily.WeatherCode.IndexOf(data));
                                        DataPrevisioni = dataPioggia.ToShortDateString();
                                        await synthesizer.SpeakTextAsync($"La condizione richiesta ci sarà il {dataPioggia.ToShortDateString()}");
                                    }
                                    else
                                    {
                                        ConvertOpacity("null");
                                        DataPrevisioni = null;
                                        await synthesizer.SpeakTextAsync("La condizione richiesta non avverrà nei prossimi giorni");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                await synthesizer.SpeakTextAsync("Errore nel recupero dei dati");
                                break;
                            }
                            break;
                        case string intent when intent.Contains("QueryWeather"):
                            try
                            {
                                MeteoVisible = true;
                                MapVisible = false;
                                MapDistance = false;
                                DistVisible = false;
                                string? condizioni = cluResponse.Result?.Prediction?.Entities?.Where(x => x.Category.ToLower().Contains("weathercondition")).FirstOrDefault()?.Text;
                                string? citta = cluResponse.Result?.Prediction?.Entities?.Where(x => x.Category.ToLower().Contains("absolutelocation") || x.Category.ToLower().Contains("placename")).FirstOrDefault()?.Text;
                                string? giorno = cluResponse.Result?.Prediction?.Entities?.Where(x => x.Category.ToLower().Contains("datetime") || x.Category.ToLower().Contains("placename")).FirstOrDefault()?.Text;
                                if (citta is null)
                                {
                                    var location = await Geolocation.GetLastKnownLocationAsync();
                                    await synthesizer.SpeakTextAsync($"Recupero informazioni meteo");
                                    var meteo = await PrevisioniMeteoCheck(location, synthesizer);
                                    int nGiorni = Giorno.CalcolaGiorno(giorno);
                                    DateTime date = DateTime.Today.AddDays(nGiorni);
                                    int? data = 0;
                                    switch (condizioni)
                                    {
                                        case string cond when cond.ToLower().Contains("pio"):
                                            data = meteo?.Daily?.WeatherCode?.Where(x => (x >= 50 && x <= 67) || (x >= 80 && x <= 82) || (x >= 95 && x <= 99)).FirstOrDefault();
                                            break;
                                        case string cond when cond.ToLower().Contains("sole"):
                                            data = meteo?.Daily?.WeatherCode?.Where(x => x == 0 || x == 1).FirstOrDefault();
                                            break;
                                        case string cond when cond.ToLower().Contains("nuvo"):
                                            data = meteo?.Daily?.WeatherCode?.Where(x => x == 2 || x == 3).FirstOrDefault();
                                            break;
                                        case string cond when cond.ToLower().Contains("nev"):
                                            data = meteo?.Daily?.WeatherCode?.Where(x => (x >= 71 && x <= 77) || (x == 85 || x == 86)).FirstOrDefault();
                                            break;
                                    }
                                    if (data > 0)
                                    {
                                        string cond = ConvertWeather.ConvertWeatherCode(data);
                                        ConvertOpacity(cond);
                                        DateTime dataPioggia = DateTime.Today.AddDays(meteo.Daily.WeatherCode.IndexOf(data));
                                        DataPrevisioni = dataPioggia.ToShortDateString();
                                        await synthesizer.SpeakTextAsync($"Si, {giorno} {condizioni}");
                                    }
                                    else
                                    {
                                        ConvertOpacity("null");
                                        DataPrevisioni = null;
                                        await synthesizer.SpeakTextAsync($"No, {giorno} non {condizioni}");
                                    }
                                }
                                else
                                {
                                    await synthesizer.SpeakTextAsync($"Recupero informazioni meteo a {citta}");
                                    var meteo = await PrevisioniMeteoCheckConCitta(citta, synthesizer);
                                    int? data = 0;
                                    switch (condizioni)
                                    {
                                        case string cond when cond.ToLower().Contains("pio"):
                                            data = meteo?.Daily?.WeatherCode?.Where(x => (x >= 50 && x <= 67) || (x >= 80 && x <= 82) || (x >= 95 && x <= 99)).FirstOrDefault();
                                            break;
                                        case string cond when cond.ToLower().Contains("sole"):
                                            data = meteo?.Daily?.WeatherCode?.Where(x => x == 0 || x == 1).FirstOrDefault();
                                            break;
                                        case string cond when cond.ToLower().Contains("nuvo"):
                                            data = meteo?.Daily?.WeatherCode?.Where(x => x == 2 || x == 3).FirstOrDefault();
                                            break;
                                        case string cond when cond.ToLower().Contains("nev"):
                                            data = meteo?.Daily?.WeatherCode?.Where(x => (x >= 71 && x <= 77) || (x == 85 || x == 86)).FirstOrDefault();
                                            break;
                                    }
                                    if (data > 0)
                                    {
                                        Citta = citta;
                                        string cond = ConvertWeather.ConvertWeatherCode(data);
                                        ConvertOpacity(cond);
                                        DateTime dataPioggia = DateTime.Today.AddDays(meteo.Daily.WeatherCode.IndexOf(data));
                                        DataPrevisioni = dataPioggia.ToShortDateString();
                                        await synthesizer.SpeakTextAsync($"Si, {giorno} a {citta} {condizioni}");
                                    }
                                    else
                                    {
                                        Citta = citta;
                                        ConvertOpacity("null");
                                        DataPrevisioni = null;
                                        await synthesizer.SpeakTextAsync($"No, {giorno} a {citta} non {condizioni}");
                                    }
                                }
                            }
                            catch(Exception ex)
                            {
                                await synthesizer.SpeakTextAsync("Errore nel recupero dei dati");
                                break;
                            }
                            break;
                        case string intent when intent.Contains("Places") && !intent.Contains("GetDistance"):
                            try
                            {
                                MeteoVisible = false;
                                MapVisible = true;
                                MapDistance = false;
                                DistVisible = false;
                                string? placeType = cluResponse.Result?.Prediction?.Entities?.Where(x => x.Category.ToLower().Contains("placetype")).FirstOrDefault()?.Text;
                                string? placeCitta = cluResponse.Result?.Prediction?.Entities?.Where(x => x.Category.ToLower().Contains("absolutelocation")).FirstOrDefault()?.Text;
                                string? nearby = cluResponse.Result?.Prediction?.Entities?.Where(x=>x.Category.ToLower().Contains("nearby")).FirstOrDefault()?.Text;
                                string? placeName = cluResponse.Result?.Prediction?.Entities?.Where(x => x.Category.ToLower().Contains("placename")).FirstOrDefault()?.Text;
                                string? traduzione = Traduzione.Traduci(placeType);
                                if (nearby is null)
                                {
                                    Citta = placeCitta;
                                    if (placeName is null)
                                    {
                                        Tipo = $"{placeType} = ";
                                        await synthesizer.SpeakTextAsync($"Recupero {placeType} a {placeCitta}");
                                        await CercaPOI(placeCitta, traduzione, synthesizer);
                                    }
                                    else
                                    {
                                        Tipo = $"{placeName} = ";
                                        await synthesizer.SpeakTextAsync($"Recupero {placeName} a {placeCitta}");
                                        await CercaPoiConNome(placeCitta, placeName, synthesizer);
                                    }

                                }
                                else
                                {
                                    var location = await Geolocation.GetLastKnownLocationAsync();
                                    Citta = await ReverseGeoCoding(location.Latitude, location.Longitude);
                                    if (placeName is null)
                                    {
                                        Tipo = $"{placeType} = ";
                                        await synthesizer.SpeakTextAsync($"Recupero {placeType} vicino a te");
                                        await CercaPoiConCoord(location, traduzione, synthesizer);
                                    }
                                    else
                                    {
                                        Tipo = $"{placeName} = ";
                                        await synthesizer.SpeakTextAsync($"Recupero {placeName} vicino a te");
                                        await CercaPoiConNomeECoord(location, placeName, synthesizer);
                                    }
                                }
                            }
                            catch(Exception ex)
                            {
                                await synthesizer.SpeakTextAsync("Errore nel recupero dei dati");
                                break;
                            }
                            break;
                        case string intent when intent.Contains("Places.GetDistance"):
                            try
                            {
                                MeteoVisible = false;
                                MapVisible = false;
                                MapDistance = false;
                                DistVisible = true;
                                CittaPartenza = cluResponse.Result?.Prediction?.Entities?.Where(x => x.Category.ToLower().Contains("placename") || x.Category.ToLower().Contains("absolutelocation")).FirstOrDefault()?.Text;
                                CittaArrivo = cluResponse.Result?.Prediction?.Entities?.Where(x => x.Category.ToLower().Contains("placename") || x.Category.ToLower().Contains("absolutelocation")).ToList()[1]?.Text;
                                await synthesizer.SpeakTextAsync($"Recupero distanza tra {CittaPartenza} e {CittaArrivo}");
                                await CalcolaPercorso(CittaPartenza, CittaArrivo, synthesizer);
                            }catch(Exception ex)
                            {
                                await synthesizer.SpeakTextAsync("Errore nel recupero dei dati");
                                break;
                            }
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
            }
        } //metodo principale

        public  async Task<string?> RecognizeIntentAsync(IntentRecognizer recognizer)
        {
            // Starts recognizing.

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
                    languageUnderstandingJSON = result.Properties.GetProperty(PropertyId.LanguageUnderstandingServiceResponse_JsonResult);
                    CLUResponse cluResponse = JsonSerializer.Deserialize<CLUResponse>(languageUnderstandingJSON, jsonSerializationOptions) ?? new CLUResponse();
                    
                    break;
                case ResultReason.Canceled:
                    var cancellation = CancellationDetails.FromResult(result);
                    break;
            }
            return languageUnderstandingJSON;
        }
        [RelayCommand]
        public async Task StartListening()
        {
            if (serviceManager != null && taskCompletionSourceManager != null)
            {
                ColoreBottone = ColoreBottone == Colors.Red ? Colors.White : Colors.Red;
                if (ColoreBottone == Colors.White)
                {
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
        public  async Task<(double? lat, double? lon)?> GetCoordinate(string? città, string language = "it", int count = 1)
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

        public void ConvertOpacity(string meteo)
        {
            switch (meteo)
            {
                case "pioggia":
                    OpSole = 0.1;
                    OpNuvole = 0.1;
                    OpPioggia = 1;
                    OpNeve = 0.1;
                    break;
                case "sole":
                    OpSole = 1;
                    OpNuvole = 0.1;
                    OpPioggia = 0.1;
                    OpNeve = 0.1;
                    break;
                case "nuvoloso":
                    OpSole = 0.1;
                    OpNuvole = 1;
                    OpPioggia = 0.1;
                    OpNeve = 0.1;
                    break;
                case "neve":
                    OpSole = 0.1;
                    OpNuvole = 0.1;
                    OpPioggia = 0.1;
                    OpNeve = 1;
                    break;
                default:
                    OpSole = 0.1;
                    OpNuvole = 0.1;
                    OpPioggia = 0.1;
                    OpNeve = 0.1;
                    break;
            }
        }
        public async Task<string> ReverseGeoCoding(double lat, double lon)
        {
            FormattableString urlRev = $"https://nominatim.openstreetmap.org/reverse?format=jsonv2&lat={lat}&lon={lon}";
            string url = FormattableString.Invariant(urlRev);
            _client.DefaultRequestHeaders.Add("User-Agent", "YourAppName/1.0 (your.email@example.com)");
            var response = await _client.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                var address = await response.Content.ReadFromJsonAsync<ReverseGeo>();
                if (address is null)
                    return null;
                return address.Address.City;
            }
            return null;
        }
    }
}
