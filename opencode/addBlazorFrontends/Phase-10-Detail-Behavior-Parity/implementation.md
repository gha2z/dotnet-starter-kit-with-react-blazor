# Phase-10 — Implementation log

Append-only. One entry per wave: what / why / evidence / lessons.

---

## P12.1 — Product detail image behaviors (2026-08-26) ✅

**What:** Product image remove now shows the React-parity "Detach this image?" confirm
dialog before deleting (cover-promotion note when the tile is the cover, "Removing…"
pending state) — the old path deleted immediately. Clicking an image tile opens a preview
modal (`ProductImagePreviewDialog.razor`, new): ≤70vh image, cover note, Close. Tiles got
the React look: cover ring + ★ COVER badge, hover-overlay actions with a touch-visible
fallback. Upload reports per-file progress ("filename · N%") in the upload status line.

**Why:** Human-reported behavioral gaps — "no confirmation dialog when you try to detach
the product images, the react shows the detach confirmation dialog" and "the react version
shows the image preview when an image is clicked, the blazor version doesn't".

**Evidence:** `probe-p12.mjs` **14/0** (upload → tile appears; click → preview dialog with
image; remove → detach confirm; confirmed remove → row gone). Screenshots in
`walkthrough/evidence/p12-behaviors/` (image-preview-dialog.png, image-remove-confirm.png).

**Lessons:**
- MudBlazor 9.7 file upload yields `IBrowserFile` (old `IFileUploadEntry` API is gone).

## P12.2 — FilePreviewDialog rebuild (2026-08-26) ✅

**What:** Full rebuild of `FilePreviewDialog.razor`: single-column layout with a single
title bar (mime icon + filename, type·size description, X); metadata card with visibility
**Switch** + consequence copy, read-only badge and detail rows (owner type/id, uploaded by,
dates); two-step inline delete confirm ("Delete this file?" → Confirm delete); Download +
Close footer. Actions are gated on the **uploader** (`CreatedByUserId == current user id`
from auth state), not on ownerType — root cause of the missing delete button reported by
the human ("no button on the dialog (picture preview) to delete the picture file"): files
uploaded under a different ownerType than "MyFiles" failed the old ownerType gate even for
their uploader. Fresh metadata refetch + fresh presigned URL per open (private files
expire); text/PDF/image previews; quarantined/pending/expired state bands; retry affordance.
Text preview moved to a shared `TextPreviewBlock.razor`.

**Two real bugs found & fixed during walkthroughs:**
1. **Text-file preview 400** — the app's `HttpClient` (auth delegating handler) attached
   JWT headers to presigned S3 GET URLs, invalidating the SigV4 signature. Fix: mint the
   URL, then fetch the body via plain-browser `fshFetchText` interop (new function in
   `fshFile.js`) with no auth headers.
2. **Text preview raced the URL mint** — the block rendered before the presigned URL was
   resolved; now it renders only after the URL is ready.

**Evidence:** `probe-p12.mjs` files section (single title, metadata card, switch for
uploader, footer actions, two-step delete, row gone after confirm) within its **14/0**;
plus a dedicated role check `probe-p12c.mjs` **8/0**: as alice (non-uploader), shared rows
owned by other users open the dialog with NO visibility switch and NO Delete, Download +
Close still present — the plan's uploader/non-uploader criterion verified both ways.

**Lessons:**
- Never send Authorization headers to presigned object-storage URLs — fetch them with a
  bare browser fetch; any middleware-injected header breaks SigV4 (HTTP 400).
- "My files" AND "Shared in tenant" both include the current user's own public files —
  role-gating probes must select a row whose `createdByUserId ≠ current user`, not just
  "first row of the shared tab".

## P12.3 — Product-detail behavioral matrix (2026-08-26) ✅

**What:** StockDialog opens at delta 0 with submit disabled; negative-stock guard shows a
banner and blocks submit; tone-colored "was → becomes" numbers with event copy.
PriceDialog shows live was→becomes preview with green/red delta coloring; currency is a
free-text field auto-uppercased and clamped to 3 chars; "Change price" label + event copy.
Page-level: refresh spins while loading (structured skeleton), meta brand/category render
as links, identifiers shown as SKU/Slug/Product ID/Brand ID/Category ID mono chips,
inventory number tone-colored with captions, pricing carries "Listed price · CUR" caption,
a dedicated not-found panel with back action replaces the blank page, and the images
section uses React's copy "Drop more to add. Star one to make it the cover."

**Evidence:** `probe-p12b.mjs` **18/0** (price was→becomes live + green delta; AED
free-text/auto-upper/clamp-3; stock guard banner + blocked submit + red/green tones +
delta-0 disabled init; ID chips; meta links; Listed price caption; not-found panel).
Screenshots: price-dialog-preview.png, stock-dialog-guard.png, not-found-panel.png.

## P12.4 — Files-page residual sweep + regression battery (2026-08-26) ✅

**What:** Side-by-side sweep of the files page against React found no further gaps beyond
P12.2's rebuild. Regression battery over the full CRUD harness plus the P12 probes.

**Evidence:** `probe-actions.mjs` **39/0** (full CRUD regression) · bUnit suite **264/264**
· probe-p12 **14/0** · probe-p12b **18/0** · probe-p12c **8/0** · 0 console errors and 0
bad network responses across all probes.

One bUnit assertion updated as expected drift:
`ProductDetailPageTests.Shows_upload_images_button_with_hint` still asserted the pre-P12.3
images-hint copy ("JPG / PNG / WebP / GIF · up to 10 MB"); it now asserts the shipped copy
("Drop more to add", "Star one to make it the cover"). Suite re-run green at 264/264.

**Mission complete.** All success criteria met; demo-DB QA-entity
leftovers remain accepted per Phase-09 P8 documentation.
