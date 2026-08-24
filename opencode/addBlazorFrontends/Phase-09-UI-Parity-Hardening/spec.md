# Spec — Phase 09: UI Parity Hardening

> Human-authored requirements. Verbatim user reports; the agent plans against this file.

## 1. Chat page (dashboard-blazor, :5176)

- Auto-scroll to the last sent message right after sending the message.
- Other users' new messages must display instantly in both DM and channel views.
- DM rail list: show partner photos when available (currently a generic person icon) and their
  online status (the small green presence dot like the React version).

## 2. Products page (dashboard-blazor)

- Brand and category filter dropdowns must match the React look-and-feel: type-to-narrow the
  dropdown list (searchable combobox), clearable.
- Edit/delete product buttons hidden until hovering a row (React behavior), but always visible
  on mobile so both stay accessible.
- Responsive columns (option A confirmed): on the Brands and Categories lists, hide slug/created
  columns on mobile; slug value moves below the description. Products list keeps React's exact
  column set (Product / SKU / Brand / Price).
- Mobile: products render as cards (React MobileCard parity).
- Product detail header card (`fsh-detail-card`) must look and feel exactly like React's
  (`EntityDetailHero`): padding, action buttons (refresh/edit/delete), layout and positions
  across sections, spacing between product images and the Browse files button.
- Add/edit/delete product/brand/category dialogs must match React (or better).

## 3. Files page (:5176/files)

- Make it look and feel like the React version (:5174/files).

## 4. Tickets page

- Filters section look-and-feel.
- BUG: clicking any filter (open/closed/low/high/etc) ends in a skeleton view with no results.

## 5. Dialogs (all CRUD)

- The dialog standardization applies to ALL CRUD dialogs (add ticket/user/role/etc and delete
  confirmations): consistent, React-matching (or better) look-and-feel. Establish a standard
  scaffold for all CRUD operations.

## 6. Audit Trail page (:5176/system/audits)

- Verify all filters work and return valid results; fix what doesn't.

## Verification requirement (user mandate)

Thorough real-browser walkthrough on every issue: real clicks, real navigation, real window
resizing (free resizes + device emulation) across desktop/mobile resolutions, executing all
available features in the affected areas, verified visually (screenshots inspected).

## Leftovers carried from Phase 08 (gap hunt)

- DM toast wording ("in Alice Nguyen:" → clearer DM phrasing).
- QA-*/Visual-* probe entities accumulate in the demo DB — cleanup.
