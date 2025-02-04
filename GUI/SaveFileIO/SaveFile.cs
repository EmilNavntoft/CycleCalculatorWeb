using CycleCalculatorWeb.CycleModel.Model.IO;
using CycleCalculatorWeb.GUI.DragDropComponents;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using Xamarin.Essentials;

namespace CycleCalculatorWeb.GUI.SaveFileIO
{
	public class SaveFile
	{
		public List<DragDropSaveFileDao> DragDropDaos { get; set; } = [];
		public List<ConnectorSaveFileDao> ConnectorDaos { get; set; } = [];

		public string Serialize()
		{
			var options = new JsonSerializerOptions();
			options.WriteIndented = true;

			return JsonSerializer.Serialize(this, options);
		}

		public static SaveFile? Deserialize(string saveFileJson)
		{
			try
			{
				var options = new JsonSerializerOptions();
				options.Converters.Add(new ObjectToInferredTypesConverter());
				SaveFile? saveFile = JsonSerializer.Deserialize<SaveFile>(saveFileJson, options);
				return saveFile;
			} 
			catch (Exception ex)
			{
				//TODO error handling
				return null;
			}
		}

		public static async Task<SaveFile> OpenLoadDialog()
		{
			var result = await FilePicker.PickAsync(new PickOptions
			{
				PickerTitle = "Please select your file.",
				FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
			{
				{ DevicePlatform.UWP, new[] { "json" } },
			})
			});
			if (result != null)
			{
				string jsonText = File.ReadAllText(result.FullPath);
				return Deserialize(jsonText);
			}
			else
			{
				return null;
			}
		}
	}	

	public class DragDropSaveFileDao
	{
		public string? Name { get; set; }
		public double X { get; set; }
		public double Y { get; set; }
		public string CycleComponentTypeString { get; set; }
		public Dictionary<string, object> CycleComponentPropertyData { get; set; } = [];

		public static DragDropSaveFileDao Build(DragDrop dragDrop)
		{
			DragDropSaveFileDao dao = new()
			{
				Name = dragDrop.Name,
				X = dragDrop.X,
				Y = dragDrop.Y,

				CycleComponentTypeString = dragDrop.CycleComponent.GetType().ToString()
			};

			var userEditableProperties = dragDrop.CycleComponent.GetType().GetProperties().Where(prop => !Attribute.IsDefined(prop, typeof(EditableAttribute)));
			foreach (var property in userEditableProperties)
			{
				dao.CycleComponentPropertyData.Add(property.Name, property.GetValue(dragDrop.CycleComponent));
			}
			return dao;
		}
	}

	public class ConnectorSaveFileDao
	{
		public string? ComponentOneName { get; set; }
		public string? ComponentTwoName { get; set; }
		public PortIdentifier PortOneIdentifier { get; set; }
		public PortIdentifier PortTwoIdentifier { get; set; }

		public static ConnectorSaveFileDao Build(Connector connector)
		{
			ConnectorSaveFileDao dao = new()
			{
				ComponentOneName = connector.ConnectionOne.Parent.Name,
				ComponentTwoName = connector.ConnectionTwo.Parent.Name,
				PortOneIdentifier = connector.ConnectionOne.PortIdentifier,
				PortTwoIdentifier = connector.ConnectionTwo.PortIdentifier
			};
			return dao;
		}
	}
}
