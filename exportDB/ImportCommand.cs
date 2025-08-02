using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Newtonsoft.Json;

namespace exportDB
{
    [Transaction(TransactionMode.Manual)]
    public class ImportCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;
            UIDocument uidoc = uiApp.ActiveUIDocument;
            Document doc = uidoc.Document;

            var task = Task.Run(() => GetDoorsFromServer());
            task.Wait(); // async → sync
            var doorDataList = task.Result;

            if (doorDataList == null)
                return Result.Failed;

            using (Transaction tx = new Transaction(doc, "문 정보 업데이트"))
            {
                tx.Start();

                foreach (Element door in new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Doors).WhereElementIsNotElementType())
                {
                    var match = doorDataList.FirstOrDefault(d => d._id == door.UniqueId);
                    if (match != null)
                    {
                        door.LookupParameter("Mark")?.Set(match.Mark ?? "");
                        door.LookupParameter("DoorFinish")?.Set(match.DoorFinish ?? "");
                    }
                }

                tx.Commit();
            }

            TaskDialog.Show("완료", "서버에서 문 정보를 불러와 업데이트했습니다.");
            return Result.Succeeded;
        }

        private async Task<List<DoorInfo>> GetDoorsFromServer()
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    var response = await client.GetAsync("http://localhost:4000/doors");

                    if (!response.IsSuccessStatusCode)
                    {
                        TaskDialog.Show("오류", "서버 응답 실패: " + response.StatusCode);
                        return null;
                    }

                    string json = await response.Content.ReadAsStringAsync();
                    var doorList = JsonConvert.DeserializeObject<List<DoorInfo>>(json);
                    return doorList;
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("예외", ex.Message);
                    return null;
                }
            }
        }

        public class DoorInfo
        {
            public string _id { get; set; } // UniqueId
            public string FamilyType { get; set; }
            public string Mark { get; set; }
            public string DoorFinish { get; set; }
        }
    }
}
