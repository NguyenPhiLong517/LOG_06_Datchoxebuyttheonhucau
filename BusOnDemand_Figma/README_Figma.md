# Bus On Demand — Figma Editable UI Pack

## Included
- 10 desktop SVG artboards (9 Form classes; `FrmDanhMuc` has two tab states: Vehicles and Configuration).
- PNG previews for fast review.
- All artboards use editable SVG shapes and text elements rather than a single embedded screenshot.

## Import into Figma
1. Unzip this package.
2. In Figma, create a new Design file.
3. Drag each SVG from the `SVG` folder onto the canvas, or use File → Place image and choose the SVG.
4. Each SVG should import as a vector group; expand it in the Layers panel to edit text, cards, buttons, chips and tables. If Figma imports a group as a single layer, right-click and use Ungroup where available.
5. Keep each 1440×900 frame separate. Rename it to its screen name.
6. Use the PNGs in `Preview` only as references; use the SVGs for editing.

## Screens
1. `01_FrmDangNhap` — username/password login only.
2. `02_FrmMain` — coordinator dashboard; actual modules vary by user role.
3. `03_FrmNhuCau` — declare, view, and cancel eligible travel needs.
4. `04_FrmDatGhe` — view eligible trips, book/cancel seat reservation; no physical seat-number selection.
5. `05_FrmDeXuatTuyen` — review grouped demand and create/reject a proposed trip.
6. `06_FrmLichTrinh` — view trips and operate on trips that have not departed.
7. `07_FrmDanhMuc_Xe` — vehicle CRUD fields.
8. `08_FrmDanhMuc_CauHinh` — second tab of `FrmDanhMuc`, editing system parameters.
9. `09_FrmThongKe` — date-filtered demand/trip/fill/no-show/accessibility summaries.
10. `10_FrmVanHanh` — trip assignment, passenger attendance, confirm departure/completion.

## Scope notes
- This is a design mockup pack derived from the report's use cases and current Form responsibilities.
- Table values are sample illustration data, not asserted live values.
- No registration/forgot-password, GPS tracking, seat-number choice, or unrequested export feature has been added.
- Dashboard artboard shows the coordinator role as an example; actual menus should be role-filtered as specified in the report.
- The pack does not replace the C# WinForms implementation.
