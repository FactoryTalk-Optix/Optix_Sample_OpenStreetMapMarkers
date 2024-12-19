// Define center point of the map
var mapLat = 42.000;
var mapLon = 13.000;
var mapZoom = 6;

function init() {

	map = new OpenLayers.Map("map", {
		controls: [
			new OpenLayers.Control.Navigation(),
			new OpenLayers.Control.PanZoomBar(),
			new OpenLayers.Control.ScaleLine(),
			new OpenLayers.Control.Permalink('permalink'),
			new OpenLayers.Control.MousePosition(),
			new OpenLayers.Control.Attribution()
		],
		projection: new OpenLayers.Projection("EPSG:900913"),
		displayProjection: new OpenLayers.Projection("EPSG:4326")
	});

	var mapnik = new OpenLayers.Layer.OSM("OpenStreetMap (Mapnik)");

	map.addLayer(mapnik);

	var lonLat = new OpenLayers.LonLat(mapLon, mapLat).transform(
		new OpenLayers.Projection("EPSG:4326"), // transform from WGS 1984
		map.getProjectionObject() // to Spherical Mercator Projection
	);

	map.setCenter(lonLat, mapZoom);

	var vectorLayer = new OpenLayers.Layer.Vector("Overlay");
	
	// Test marker
	//markersList.push({Latitude: 10.4713, Longitude: 47.1723, Comment: 'test', GUID: "000000000000000000"});

	// Create markers coming from the web server
	for (var i = 0; i < markersList.length; i++) {
		if (!isNaN(markersList[i]['Latitude']) && !isNaN(markersList[i]['Longitude'])) {
			var eGraphic = 'img/marker.svg';
			var feature = new OpenLayers.Feature.Vector(
				new OpenLayers.Geometry.Point(markersList[i]['Latitude'], markersList[i]['Longitude'])
					.transform(new OpenLayers.Projection('EPSG:4326'),
						map.getProjectionObject()),
						{
							description: '<strong>' + markersList[i]['Comment'] + '</strong>'
						},
						{
							externalGraphic: eGraphic,
							graphicHeight: 25,
							graphicWidth: 21,
							graphicXOffset: -12,
							graphicYOffset: -25
						}
					);
			vectorLayer.addFeatures(feature);
		} else {
			console.log("Marker " + markersList[i]['GUID'] + " will be ignored");
		}
	}

	// Add markers layer
	map.addLayer(vectorLayer);

	// Add mouse handler for all controls
	var controls = {
		selector: new OpenLayers.Control.SelectFeature(vectorLayer, {
			onSelect: createPopup,
			onUnselect: destroyPopup
		})
	};

	// Open/close callback for each marker
	function createPopup(feature) {
		feature.popup = new OpenLayers.Popup.FramedCloud("pop",
			feature.geometry.getBounds().getCenterLonLat(),
			null,
			'<div class="markerContent">' + feature.attributes.description + '</div>',
			null,
			true,
			function() {
				controls['selector'].unselectAll();
			}
		);

		map.addPopup(feature.popup);
	}

	function destroyPopup(feature) {
		feature.popup.destroy();
		feature.popup = null;
	}

	// Register callbacks
	map.addControl(controls['selector']);
	controls['selector'].activate();
}