using UnityEngine;
using UnityEngine.UI;

public class WeatherUILinker : MonoBehaviour
{
    void Start()
    {
        WeatherManager wm = WeatherManager.Instance;
        if (wm == null) return;

        if (wm.timeText == null) wm.timeText = FindText("TimeDisplay");
        if (wm.weatherText == null) wm.weatherText = FindText("WeatherDisplay");

        LinkButton("Btn_Pagi", () => wm.SetTime(WeatherManager.TimeOfDay.Pagi));
        LinkButton("Btn_Siang", () => wm.SetTime(WeatherManager.TimeOfDay.Siang));
        LinkButton("Btn_Sore", () => wm.SetTime(WeatherManager.TimeOfDay.Sore));
        LinkButton("Btn_Malam", () => wm.SetTime(WeatherManager.TimeOfDay.Malam));
        LinkButton("Btn_Auto", () => wm.ToggleAutoCycle());
        LinkButton("Btn_Hujan/Cerah", () => wm.ToggleWeather());
    }

    static Text FindText(string name)
    {
        GameObject obj = GameObject.Find(name);
        return obj != null ? obj.GetComponent<Text>() : null;
    }

    void LinkButton(string name, UnityEngine.Events.UnityAction action)
    {
        GameObject obj = GameObject.Find(name);
        if (obj != null)
        {
            Button btn = obj.GetComponent<Button>();
            if (btn != null)
                btn.onClick.AddListener(action);
        }
    }
}
