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
            var door = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance))
                .OfCategory(BuiltInCategory.OST_Doors).FirstOrDefault();

            if (door == null)
            {
                TaskDialog.Show("결과", "문이 없습니다.");
                return Result.Succeeded;
            }
            TaskDialog.Show("결과", "Test");
            // 정보 추출
            string id = door.UniqueId;
            string familyType = door.Name;
            string mark = door.LookupParameter("Mark")?.AsString();
            string finish = door.LookupParameter("DoorFinish")?.AsString();

            // 서버로 전송
            Task.Run(async () =>
            {
                await SendDoorDataToServer(id, familyType, mark, finish);
            });

            TaskDialog.Show("결과", "서버로 전송 시도 완료");
            return Result.Succeeded;
        }

        private async Task SendDoorDataToServer(string id, string type, string mark, string finish)
        {
            var doorData = new
            {
                _id = id,
                FamilyType = type,
                Mark = mark,
                DoorFinish = finish
            };

            string json = JsonConvert.SerializeObject(doorData);

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = await client.PostAsync("http://localhost:4000/doors", content);

                    if (!response.IsSuccessStatusCode)
                    {
                        TaskDialog.Show("오류", "서버 응답 실패: " + response.StatusCode);
                    }
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("예외", ex.Message);
                }
            }
        }

        //public static Result ExportBatch(FilteredElementCollector doors, ref string message)
        //{
        //    List<Door> doorData = new List<Door>();

        //    HttpStatusCode statusCode;
        //    string jsonResponse, errorMessage;
        //    Result result = Result.Succeeded;

        //    foreach (Element element in doors)
        //    {
        //        doorData.Add(new DoorData(element));
        //    }

        //    //REST request to batch post door data
        //    statusCode = DoorAPI.PostBatch(out jsonResponse, out errorMessage, "doors", doorData);

        //    if ((int)statusCode == 0)
        //    {
        //        message = errorMessage;
        //        result = Result.Failed;
        //    }

        //    return result;

        //}
    }
}
