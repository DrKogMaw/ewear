---
name: Ewear
description: A sunlit, single-sans fashion catalog with clear shopping feedback and a quiet dark theme.
colors:
  peach: "#FFD3AC"
  blush: "#FFB5AB"
  clay: "#E39A7B"
  gold: "#DBB06B"
  espresso: "#3B2924"
  ink: "#35251E"
  canvas: "#FFF8F2"
  muted-ink: "#65483C"
  line: "#D9AE9E"
  field-line: "#BB8D79"
  error: "#8D3028"
  success: "#226347"
  dark-canvas: "#251F1C"
  dark-ink: "#EADCD0"
typography:
  display:
    fontFamily: "DM Sans, sans-serif"
    fontSize: "clamp(60px, 6vw, 90px)"
    fontWeight: 500
    lineHeight: 1.02
    letterSpacing: "-.035em"
  headline:
    fontFamily: "DM Sans, sans-serif"
    fontSize: "clamp(42px, 4.4vw, 66px)"
    fontWeight: 500
    lineHeight: 1
    letterSpacing: "-.035em"
  title:
    fontFamily: "DM Sans, sans-serif"
    fontSize: "17px"
    fontWeight: 600
  body:
    fontFamily: "DM Sans, sans-serif"
    fontSize: "16px"
    lineHeight: 1.6
  label:
    fontFamily: "DM Sans, sans-serif"
    fontSize: "14px"
    fontWeight: 700
    letterSpacing: ".02em"
rounded:
  square: "0"
  dot: "50%"
spacing:
  xxs: "4px"
  xs: "8px"
  sm: "12px"
  md: "20px"
  lg: "24px"
  xl: "32px"
  section: "100px"
components:
  button-hero:
    backgroundColor: "{colors.espresso}"
    textColor: "{colors.canvas}"
    rounded: "{rounded.square}"
    height: "60px"
    padding: "0"
  button-hero-arrow:
    backgroundColor: "{colors.gold}"
    textColor: "{colors.ink}"
    height: "60px"
    width: "60px"
  button-dark:
    backgroundColor: "{colors.espresso}"
    textColor: "{colors.canvas}"
    rounded: "{rounded.square}"
    height: "52px"
    padding: "0 23px"
  button-quick-add:
    backgroundColor: "{colors.peach}"
    textColor: "{colors.ink}"
    rounded: "{rounded.square}"
    height: "48px"
    padding: "0 16px"
  button-quick-add-success:
    backgroundColor: "{colors.success}"
    textColor: "#ffffff"
    height: "48px"
  input:
    backgroundColor: "#ffffff"
    textColor: "{colors.ink}"
    rounded: "{rounded.square}"
    height: "48px"
    padding: "11px 13px"
  tag:
    backgroundColor: "{colors.blush}"
    textColor: "{colors.ink}"
    rounded: "{rounded.square}"
    padding: "9px 11px"
---

# Design System: Ewear

## Overview

**Creative North Star: "The Sunlit Wardrobe"**

Ewear presents everyday shirts, pants, and boots as a considered fashion edit. Natural photography stays prominent while the user-selected palette warms the surrounding interface. Peach and blush carry larger surfaces; gold signals the primary entry into shopping; clay marks smaller moments of emphasis. Espresso ink anchors the light colors and keeps text readable.

DM Sans carries every heading, control, and product detail. Weight and space create the hierarchy. The same color roles continue through the cart, account, and staff screens, with a lower-contrast dark theme for evening browsing.

**Key Characteristics:**

- Sunlit lifestyle and product photography framed by warm peach surfaces.
- A four-color palette used with dark ink and a pale reading canvas.
- One sans-serif family with deliberate shifts in weight and scale.
- Square controls, fine dividers, and visible product actions.
- Inline confirmation after adding to the bag; the bag opens on request.

## Colors

The four supplied colors are the recognizable identity. Supporting dark and pale neutrals supply contrast for text and form controls.

### Primary

- **Peach:** Header, introduction strip, dialog headers, and account dialog.
- **Gold:** Announcement strip and the arrow section of the hero shopping action.

### Secondary

- **Blush:** Featured tags, quick-add hover, and hero arrow hover.
- **Clay:** Brand dot and small editorial accents.

### Neutral

- **Espresso:** Dark buttons, footer, toast, and strong control borders.
- **Ink:** Body and heading text over all four brand colors.
- **Canvas:** Main collection, cart body, and staff form surfaces.
- **Muted Ink:** Supporting text over the canvas and brand surfaces.
- **Line / Field Line:** Warm separators and input borders.
- **Error:** Form feedback and error toasts.
- **Success:** Deep green only for confirmed actions such as adding to the bag.

**The Photo Leads Rule.** Keep product and lifestyle photographs untinted; place the palette around them and in controls.

## Typography

**Display and body font:** DM Sans, with a sans-serif fallback. All text uses this one family.

**Character:** Medium display weight, bold phrase emphasis, and restrained tracking give the storefront an editorial voice without a second typeface.

### Hierarchy

- **Display:** Medium-weight hero text with a bold second phrase. At narrow widths it changes to `clamp(55px, 14vw, 75px)`.
- **Headline:** Medium-weight collection heading.
- **Lead:** Hero description at 18px with a 430px maximum measure; 16px on phones.
- **Title:** Semibold product names at 17px on desktop and 15px on phones, with prices at 16px and 15px.
- **Body:** Reading and form copy at 16px with generous line height. Product metadata is 14px on desktop and 13px on phones.
- **Label:** Controls and form labels at 14–15px. Only short uppercase accents, badges, and footer details use 11–13px.

**The Single Family Rule.** Use DM Sans throughout. Differentiate emphasis with weight, scale, and spacing.

The peach introduction strip gives its three phrases 15px type on desktop and 14px on narrow screens. They stay visible on phones, where the decorative stars drop away as the phrases wrap.

## Layout

The header and announcement form full-width color bands. The hero remains a wide photographic field with a peach linear gradient behind its message. The collection has a 1600px maximum width and three columns with 24px horizontal and 48px vertical gaps. Product photographs keep a 0.81 aspect ratio and carry visible add controls. Main spacing follows a 4px rhythm with more space between sections than within a product card.

At 850px, navigation moves into a menu and products become two columns. At 560px, the hero becomes 720px tall, collection side padding becomes 20px, and product metadata stacks. The footer stacks vertically. Cart and staff drawers use the full viewport width when needed.

## Elevation & Depth

The catalog stays flat. Color bands, photographs, and fine rules establish hierarchy. Soft shadows appear only on temporary layers: the mobile menu, drawers, account dialog, and toast. Product images scale slightly on hover; the hero arrow moves up and right while its two-part action rises 3px. Adding to the bag briefly turns that product action green, animates the count, and shows a confirmation near the bottom of the viewport. Reduced-motion settings shorten these effects.

**The Layered Only When Open Rule.** Keep resting product cards flat; reserve shadows for active overlays and feedback.

## Shapes

Buttons, tags, inputs, dialogs, and panels use square corners. Thin borders define fields, tabs, and lists. The small round clay dot in the wordmark is the one recurring circular mark.

## Components

### Buttons

- **Hero action:** Espresso label segment and gold arrow segment, both 60px high. Hover changes the arrow to blush and moves it diagonally while the action rises 3px.
- **Header sign in:** A compact espresso label and gold arrow echo the hero action. The mobile menu gives account entry a full-width version; dark mode uses a gold label for contrast.
- **Form and bag actions:** Espresso fill with pale text, square edges, and 52px minimum height.
- **Quick add:** Peach rectangle visible at the bottom of every product photo; blush on hover. It remains at least 44px high on phones.
- **Added state:** Green action with a check icon and a small confirmation containing a voluntary View bag control. Adding never opens the drawer.
- **Text controls:** Unfilled, with dark ink and clear underlines or rules. Focus uses a visible warm outline.

### Catalog

- **Header navigation:** Editorial category labels share a fine baseline in the desktop header. The current category has a stronger underline; the mobile menu uses open rows with fine dividers and arrows.
- **Collection heading:** Reflects the selected category while the header menu handles category filtering.
- **Featured tag:** Small uppercase blush label over the photograph.
- **Product card:** No outer border or shadow. The photo, quick add, title, color/category, and price remain visually distinct.

### Forms and Overlays

- **Inputs:** White fields with warm borders, square edges, and 48px minimum height. Focus darkens the border.
- **Bag and staff drawers:** Pale canvas bodies with peach headers, warm rules, and a dimmed backdrop.
- **Account:** Centered peach dialog with a white phone field and a full-width dark action.

### Dark Mode

The theme switch in the header saves the visitor's choice. Dark mode uses desaturated espresso surfaces, warm light text, and restrained gold highlights. The hero retains its peach gradient and dark text so the photograph remains readable. Product photography stays unchanged. Dark controls use clear borders and focus rings rather than extra shadows.

### Feedback

Success uses green, errors use red, and neutral information uses espresso. Toasts enter from below and remain short enough to leave browsing visible. Buttons respond to hover, press, focus, disabled, and busy states. A selected category is marked in the header and mobile menu, and named by the collection heading.

## Do's and Don'ts

### Do:

- **Do** use the supplied peach, blush, clay, and gold in their established roles.
- **Do** keep dark ink over light brand colors and preserve readable contrast.
- **Do** keep product images large and add actions visible.
- **Do** preserve keyboard focus and reduced-motion behavior.
- **Do** confirm cart additions in place and let shoppers open the bag when ready.

### Don't:

- **Don't** tint the product photography to force a color match.
- **Don't** add rounded cards or shadows to resting product cards.
- **Don't** hide the add action until hover.
- **Don't** open the bag automatically after adding an item.
