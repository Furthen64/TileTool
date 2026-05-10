# TileTool

TileTool lets you select and extract tile-sized regions from larger images.

## Quick Start

1. Build and run:
   - `dotnet build`
   - `dotnet run --project TileTool/TileTool.csproj`
2. Choose an image.
3. Choose an output folder.
4. Drag or resize the selection rectangle.
5. Press `Space` to save a tile.

## Features

- Interactive selection rectangle with drag/resize handles
- Optional grid snapping after first save
- Per-output-folder config (`tiletool.json`) for prefix/default size/index
- Typed size input directly on the canvas (`WxH`, then Enter)
- Sequential PNG export with safe filename sanitization

## Build

- Linux helper script: `build.sh`
- Windows helper script: `winbuild.ps1`
- Cross-platform CLI: `dotnet build`

## Known Limits

- Supports local filesystem paths from system pickers only.
- Saves PNG output only.
- Selection is clamped to image bounds; sub-pixel precision is rounded to pixels.

## Troubleshooting

- **Cannot save tile**: verify output-folder permissions, free disk space, and writable path.
- **Config reset notice**: `tiletool.json` was unreadable/invalid and defaults were re-created.
- **Image cannot load**: ensure the selected file is a readable image format (`png`, `jpg`, `jpeg`, `bmp`, `gif`, `webp`).
