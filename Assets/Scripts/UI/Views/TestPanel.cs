using System.Collections.Generic;
using Map;
using TMPro;
using UnityEngine;

public class TestPanel : MonoBehaviour
{
    [SerializeField] private GameObject testPanel;
    [SerializeField] private GameObject companyPrefab;

    private List<Transform> _companyItems = new List<Transform>();

    public void ShowCompanyPanel()
    {
        foreach (var item in _companyItems)
        {
            Destroy(item.gameObject);
        }

        testPanel.SetActive(true);

        var pos = GameManager.I.CurrentAgent.transform.position;
        var chunk = MapManager.I.CartonMap.GetChunk(pos, Chunk.CityLayer);

        if (chunk != null)
        {
            var city = chunk.City;
            if (city != null && GameManager.I.CitizenManager.Companies.TryGetValue(city, out var companies))
            {
                Log.LogInfo("TestPanel", $"城市 {city} 的公司数量: {companies.Count}");
                foreach (var family_company in companies)
                {
                    var companyItem = Instantiate(companyPrefab, testPanel.transform);
                    companyItem.GetComponent<TextMeshProUGUI>().text = family_company.Value.CompanyName;
                    _companyItems.Add(companyItem.transform);
                }
            }
        }
    }
}