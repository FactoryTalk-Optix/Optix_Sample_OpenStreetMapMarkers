#region Using directives
using System;
using System.Linq;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.Store;
using FTOptix.UI;
using UAManagedCore;
#endregion

public class MarkersEditLogic : BaseNetLogic
{
    public override void Start()
    {
        // Insert code to be executed when the user-defined logic is started
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }

    [ExportMethod]
    public void InsertNewMarker()
    {
        // Get the database from the grid
        var store = GetStoreFromDataGrid();
        var table = GetTableFromStore(store);
        string[] columns = ["Latitude", "Longitude", "Comment", "GUID"];
        object[,] values = new object[1, 4];
        values[0, 0] = GetLatitudeValue();
        values[0, 1] = GetLongitudeValue();
        values[0, 2] = GetCommentValue();
        values[0, 3] = Guid.NewGuid().ToString().Replace("-", "");

        if (values[0, 2] == null || values[0, 2].ToString().Length == 0)
        {
            Log.Error("InsertNewMarker", "Comment is empty");
            return;
        }

        try
        {
            table.Insert(columns, values);
            Log.Info("InsertNewMarker", $"New marker inserted, latitude: {values[0, 0]}, longitude: {values[0, 1]}, comment: {values[0, 2]}");
        }
        catch (Exception e)
        {
            Log.Error("InsertNewMarker", $"Error inserting new marker: {e.Message}");
        }
    }

    [ExportMethod]
    public void UpdateMarker()
    {
        // Get the database from the grid
        var store = GetStoreFromDataGrid();
        var latitude = GetLatitudeValue();
        var longitude = GetLongitudeValue();
        var comment = GetCommentValue();
        var guid = GetSelectedGUID();
        try
        {
            store.Query($"UPDATE Markers SET Latitude = {latitude}, Longitude = {longitude}, Comment = \"{comment}\" WHERE GUID = \"{GetSelectedGUID()}\"",
                out String[] header, out Object[,] resultSet);
            Log.Info("UpdateMarker", $"Marker updated, latitude: {latitude}, longitude: {longitude}, comment: {comment}");
        }
        catch (Exception e)
        {
            Log.Error("UpdateMarker", $"Error updating marker: {e.Message}");
        }
    }

    [ExportMethod]
    public void SelectionChanged()
    {
        var selectedRow = GetSelectedRow();
        SetLatitudeValue(Convert.ToDouble(selectedRow.Children.OfType<IUAVariable>().FirstOrDefault(static x => x.BrowseName == "Latitude")?.Value.Value));
        SetLongitudeValue(Convert.ToDouble(selectedRow.Children.OfType<IUAVariable>().FirstOrDefault(static x => x.BrowseName == "Longitude")?.Value.Value));
        SetCommentValue(selectedRow.Children.OfType<IUAVariable>().FirstOrDefault(static x => x.BrowseName == "Comment")?.Value.Value.ToString());
    }

    [ExportMethod]
    public void DeleteMarker()
    {
        // Get the database from the grid
        var store = GetStoreFromDataGrid();
        try
        {
            store.Query($"DELETE FROM Markers WHERE GUID = \"{GetSelectedGUID()}\"",
                out String[] header, out Object[,] resultSet);
            Log.Info("DeleteMarker", $"Marker deleted");
        }
        catch (Exception e)
        {
            Log.Error("DeleteMarker", $"Error deleting marker: {e.Message}");
        }
    }

    [ExportMethod]
    public void AddRandomMarkers()
    {
        var store = GetStoreFromDataGrid();
        var table = GetTableFromStore(store);
        Random random = new Random();
        string[] columns = ["Latitude", "Longitude", "Comment", "GUID"];
        object[,] values = new object[10, 4];
        for (int i = 0; i < values.GetLength(0); i++)
        {
            values[i, 0] = (random.NextDouble() * 180) - 90;
            values[i, 1] = (random.NextDouble() * 360) - 180;
            values[i, 2] = "Random marker " + i.ToString();
            values[i, 3] = Guid.NewGuid().ToString().Replace("-", "");
        }
        try
        {
            table.Insert(columns, values);
            Log.Info("AddRandomMarkers", $"New markers inserted");
        }
        catch (Exception e)
        {
            Log.Error("AddRandomMarkers", $"Error inserting new markers: {e.Message}");
        }
    }

    private Store GetStoreFromDataGrid()
    {
        Store store = InformationModel.Get<Store>(Owner.GetVariable("DataGrid/Model").Value);
        if (store == null)
        {
            throw new ArgumentException("Store not found");
        }
        else
        {
            return store;
        }
    }

    private static Table GetTableFromStore(Store store)
    {
        Table table = store.Tables.Get("Markers");
        if (table == null)
        {
            throw new ArgumentException("Table not found");
        }
        else
        {
            return table;
        }
    }

    private double GetLatitudeValue()
    {
        return Convert.ToDouble(Owner.Get<SpinBox>("Latitude").Value);
    }

    private void SetLatitudeValue(double value)
    {
        Owner.Get<SpinBox>("Latitude").Value = value;
    }

    private double GetLongitudeValue()
    {
        return Convert.ToDouble(Owner.Get<SpinBox>("Longitude").Value);
    }

    private void SetLongitudeValue(double value)
    {
        Owner.Get<SpinBox>("Longitude").Value = value;
    }

    private string GetCommentValue()
    {
        return Owner.Get<TextBox>("Comment").Text;
    }

    private void SetCommentValue(string value)
    {
        Owner.Get<TextBox>("Comment").Text = value;
    }

    private string GetSelectedGUID()
    {
        var selectedRow = GetSelectedRow();

        var rowChildren = selectedRow.Children;
        if (rowChildren?.Any() != true)
        {
            throw new ArgumentException("Row has no children");
        }

        var guidChild = rowChildren.OfType<IUAVariable>().FirstOrDefault(static x => x.BrowseName == "GUID") ?? throw new ArgumentException("GUID not found");

        return guidChild.Value.Value.ToString();
    }

    private IUANode GetSelectedRow()
    {
        var grid = Owner.Get<DataGrid>("DataGrid") ?? throw new ArgumentException("DataGrid not found");

        return InformationModel.Get(grid.UISelectedItem) ?? throw new ArgumentException("No row selected");
    }
}
