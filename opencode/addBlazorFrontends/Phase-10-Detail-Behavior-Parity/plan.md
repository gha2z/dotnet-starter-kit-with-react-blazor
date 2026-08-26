# Phase-10 — Detail Behavior Parity (P12)

> Human requirements (verbatim from chat, 2026-08-26):
> - "check on the product detail page again and find the behavioral gaps … both are running"
> - "no confirmation dialog when you try to detach the product images, the react shows the
>   detach confirmation dialog"
> - "the react version shows the image preview when an image is clicked, the blazor version
>   doesn't"
> - "eliminate all gaps on files page … along with all the relevant dialogs such as no button
>   on the dialog (picture preview) to delete the picture file and the layout looks messy"
> - Plus the earlier behavioral matrix: stock/price dialog behaviors, refresh spin, meta
>   links, identifier chips, inventory tone, not-found panel.

## Success criteria

- Product image remove shows the "Detach this image?" confirm (cover-promotion note when
  cover, "Removing…" pending) — never an immediate delete.
- Clicking a product image opens a preview modal (≤70vh image, cover note, Close).
- Image tiles: cover ring + ★ COVER badge, hover-overlay actions (visible on touch).
- Upload shows per-file progress ("filename · N%").
- FilePreviewDialog: uploader-gated actions (`CreatedByUserId == current user`, NOT
  ownerType), single-column layout, metadata card with visibility Switch + consequence
  copy, two-step inline delete confirm, Download + Close footer, fresh metadata/URLs per
  open, text/PDF/image previews, quarantined/pending/expired states.
- Stock dialog: opens at delta 0 (submit disabled), negative-stock guard banner + blocked
  submit, tone-colored "becomes", event copy.
- Price dialog: was→becomes live preview with delta coloring, free-text uppercase currency
  (max 3), "Change price" label + event copy.
- Refresh spins while loading; meta brand/category are links; identifiers show
  SKU/Slug/Product ID/Brand ID/Category ID mono chips; inventory number tone-colored with
  captions; pricing "Listed price · CUR" caption; dedicated not-found panel with back
  action; images-section copy; structured loading skeleton.
- Real-browser walkthrough of every behavior (uploader + non-uploader roles), desktop +
  mobile; bUnit 264/264; probe-actions 39/0; 0 console errors.

## Status markers: ☐ pending · 🔄 in progress · ✅ done

### P12.1 — Product detail image behaviors — ✅
 - [x] Remove-confirm dialog (detach) — ☐
 - [x] Click-to-preview modal — ☐
 - [x] Tile polish: cover ring, hover overlay, touch fallback — ☐
 - [x] Upload per-file progress — ☐

### P12.2 — FilePreviewDialog rebuild — ✅
 - [x] Uploader gate via CreatedByUserId (+ current-user id via auth state) — ☐
 - [x] Single-column layout, single title (mime icon + name, type·size desc, X) — ☐
 - [x] Metadata card: visibility Switch + copy, read-only badge, detail rows — ☐
 - [x] Two-step inline delete confirm + Download + Close footer — ☐
 - [x] Robustness: metadata refetch, fresh presigned URL, text preview, state bands, retry — ☐

### P12.3 — Product detail behavioral matrix — ✅
 - [x] StockDialog: delta-0 init, negative guard, tones, copy — ☐
 - [x] PriceDialog: was→becomes live, currency free-text, copy — ☐
 - [x] Refresh spin · meta links · identifiers chips · inventory tone · pricing caption ·
      not-found panel · images copy · skeleton — ☐

### P12.4 — Files page residual sweep + verification — ✅
 - [x] Side-by-side files page sweep (desktop+mobile) — ☐
 - [x] Full walkthrough (all behaviors, both roles) + regression battery — ☐
