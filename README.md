# OpenStreetMap integration in FactoryTalk Optix

This demo shows how to handle a set of markers coming from a local database and display them using OpenStreetMap layers

## Disclaimer

Rockwell Automation maintains these repositories as a convenience to you and other users. Although Rockwell Automation reserves the right at any time and for any reason to refuse access to edit or remove content from this Repository, you acknowledge and agree to accept sole responsibility and liability for any Repository content posted, transmitted, downloaded, or used by you. Rockwell Automation has no obligation to monitor or update Repository content

The examples provided are to be used as a reference for building your own application and should not be used in production as-is. It is recommended to adapt the example for the purpose, observing the highest safety standards.

## Dependencies

- This project relies on the APIs of OpenStreetMap, please check the copyright and licenses on the [OpenStreetMap](https://www.openstreetmap.org/copyright) website

## Usage

- Navigate to the `Markers` page and manage the list of markers
- Navigate to the `Map` page to show the map with the set of markers
    - The map can be zoomed and panned using gestures
    - Clicking on a marker will display the comment that was inserted

## Description

- The project will open a WebServer listening on the port `4088` which is used to handle requests
- Every time the index page is requested, a `SELECT` query is executed in the database to return a `.js` file containing all the markers
