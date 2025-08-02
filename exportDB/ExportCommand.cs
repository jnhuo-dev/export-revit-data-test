using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using Newtonsoft.Json;

namespace exportDB
{
    [Transaction(TransactionMode.ReadOnly)]
    public class ExportCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            //Get the Current Session / Project from Revit
            UIApplication uiapp = commandData.Application;

            //Get the Current Document from the Current Session
            Document doc = uiapp.ActiveUIDocument.Document;

            //Get all doors from project
            var collector = new FilteredElementCollector(doc)
                                .OfCategory(BuiltInCategory.OST_Doors)
                                .WhereElementIsNotElementType();

            var doorList = new List<object>();

            foreach (Element door in collector)
            {
                var info = new
                {
                    _id = door.Id.Value.ToString(),
                    FamilyType = door.Name,
                    Mark = door.LookupParameter("Mark")?.AsString(),
                    DoorFinish = door.LookupParameter("DoorFinish")?.AsString()
                };
                doorList.Add(info);
            }

            if (doorList.Count == 0)
            {
                TaskDialog.Show("결과", "문이 없습니다.");
                return Result.Succeeded;
            }

            // 서버로 전송
            Task.Run(async () =>
            {
                await SendDoorDataToServer(doorList);
            });

            TaskDialog.Show("결과", "서버로 전송 시도 완료");
            return Result.Succeeded;
        }

        private async Task SendDoorDataToServer(List<Object> doorList)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    foreach (var door in doorList)
                    {
                        string json = JsonConvert.SerializeObject(door);
                        var content = new StringContent(json, Encoding.UTF8, "application/json");
                        var response = await client.PostAsync("http://localhost:4000/doors", content);

                        if (!response.IsSuccessStatusCode)
                        {
                            TaskDialog.Show("오류", "서버 응답 실패: " + response.StatusCode);
                        }
                        else
                        {
                            TaskDialog.Show("성공", "여러 문 정보 전송 완료!");
                        }
                    }
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("예외", ex.Message);
                }
            }
        }
    }
}
