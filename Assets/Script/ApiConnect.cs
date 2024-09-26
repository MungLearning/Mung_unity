using System.Collections;
using System.Text;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class ApiKeyData
{
    public string Encoding;
}

[Serializable]
public class RequestData
{
    public string serviceKey;
    // 추 후 UI를 통해 입력 받을 값들
    public string endde = "20240925";
    public string pageNo = "1"; 

    public string upkind = "417000";  
    public string upr_cd = "6440000"; 
    public string org_cd = "4490000";
    public string state = "protect"; 
    public string neuter_yn = null; 
}

[Serializable]
public class ApiResponse
{
    public Response response;
}

[Serializable]
public class Response
{
    public Header header;
    public Body body;
}

[Serializable]
public class Header
{
    public string reqNo;
    public string resultCode;
    public string resultMsg;
}


[Serializable]
public class Body
{
    public ItemWrapper items; 
}

[Serializable]
public class ItemWrapper
{
    public Item[] item; 
}

[Serializable]
public class Item
{
    public string desertionNo;
    public string filename;
    public string happenDt;
    public string happenPlace;
    public string kindCd;
    public string colorCd;
    public string age;
    public string weight;
    public string sexCd;
    public string neuterYn;
    public string specialMark;
    public string noticeNo;
    public string noticeSdt;
    public string noticeEdt;
    public string popfile;
    public string processState;
    public string careNm;
    public string careTel;
    public string careAddr;
    public string orgNm;
    public string chargeNm;
    public string officetel;
}

public class ApiConnect : MonoBehaviour
{
    private string baseUrl = "http://apis.data.go.kr/1543061/abandonmentPublicSrvc/abandonmentPublic";
    private float retryDelay = 10.0f;
    private int maxRetries = 3;
    private int currentRetries = 0;

    void Start()
    {
        StartCoroutine(Request());
    }

    public IEnumerator Request()
    {
        RequestData requestData = new RequestData();
        string json = JsonUtility.ToJson(requestData);
        
        string apiKeyJson = File.ReadAllText(Application.dataPath + "/Json/ApiKey.json");
        ApiKeyData apiKeyData = JsonUtility.FromJson<ApiKeyData>(apiKeyJson);
        requestData.serviceKey = apiKeyData.Encoding;
        
        string url = $"{baseUrl}?bgnde=20240101&endde={requestData.endde}&pageNo={requestData.pageNo}&numOfRows=1000" +
                     $"&upkind={requestData.upkind}&upr_cd={requestData.upr_cd}&org_cd={requestData.org_cd}"+
                     $"&state={requestData.state}&neuter_yn={requestData.neuter_yn}"+
                     $"&serviceKey={requestData.serviceKey}&_type=json";
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                if (www.responseCode == 429 && currentRetries < maxRetries)
                {
                    currentRetries++;
                    Debug.Log("Rate limit reached. Waiting for " + retryDelay + " seconds. Retry count: " + currentRetries);
                    yield return new WaitForSeconds(retryDelay);
                    StartCoroutine(Request());
                }
                else
                {
                    Debug.LogError("Error: " + www.error);
                }
            }
            else
            {
                Debug.Log(www.downloadHandler.text);
                ApiResponse apiResponse = JsonUtility.FromJson<ApiResponse>(www.downloadHandler.text);
                if (apiResponse != null && apiResponse.response != null && apiResponse.response.body != null &&
                apiResponse.response.body.items != null && apiResponse.response.body.items.item != null)
                {
                    SaveItemsToCSV(apiResponse.response.body.items.item); 
                }
                else
                {
                    Debug.LogError("Error: Response data is incomplete or null.");
                }
            }
        }
    }
    void SaveItemsToCSV(Item[] items)
    {

        if (items == null || items.Length == 0)
        {
            Debug.LogError("No items to save.");
            return;
        }
        string filePath = Path.Combine(Application.dataPath, "CSV/items.csv");

        using (StreamWriter writer = new StreamWriter(filePath, false, Encoding.UTF8))
        {
            writer.WriteLine("유기번호,발견장소,품종,색상,나이,체중,성별,중성화여부,특징");
            foreach (Item item in items)
            {
                string csvLine = $"\"{item.desertionNo}\",\"{item.happenPlace}\",\"{item.kindCd}\",\"{item.colorCd}\",\"{item.age}\",\"{item.weight}\",\"{item.sexCd}\",\"{item.neuterYn}\",\"{item.specialMark}\"";

                writer.WriteLine(csvLine);
            }

            Debug.Log("CSV 파일 저장 완료: " + filePath);
        }

    }
}


