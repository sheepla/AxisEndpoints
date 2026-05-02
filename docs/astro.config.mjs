// @ts-check
import { defineConfig } from "astro/config";
import starlight from "@astrojs/starlight";
import starlightThemeNord from "starlight-theme-nord";
import react from "@astrojs/react";
import tailwindcss from "@tailwindcss/vite";
import mermaid from "astro-mermaid";

// https://astro.build/config
export default defineConfig({
  integrations: [
    mermaid(),
    starlight({
      title: "AxisEndpoints",
      social: [
        {
          icon: "github",
          label: "GitHub",
          href: "https://github.com/sheepla/AxisEndpoints",
        },
      ],
      sidebar: [
        {
          label: "Getting Started",
          items: [
            { label: "Installation", slug: "getting-started/installation" },
            { label: "Quick Start", slug: "getting-started/quick-start" },
          ],
        },
        {
          label: "Guides",
          items: [
            { label: "Core Primitives", slug: "guides/core-primitives" },
            { label: "Defining Endpoints", slug: "guides/defining-endpoints" },
            { label: "Request Binding", slug: "guides/request-binding" },
            { label: "Response Types", slug: "guides/response-types" },
            { label: "Validation", slug: "guides/validation" },
            { label: "Authorization", slug: "guides/authorization" },
            { label: "Error Responses", slug: "guides/error-responses" },
            { label: "Endpoint Groups", slug: "guides/endpoint-groups" },
            { label: "Filters", slug: "guides/filters" },
            { label: "HTTP Context", slug: "guides/http-context" },
          ],
        },
        {
          label: "Extensions",
          items: [
            { label: "CSV Helper", slug: "extensions/csv-helper" },
            { label: "CSV Import", slug: "extensions/csv-helper/csv-import" },
            { label: "CSV Export", slug: "extensions/csv-helper/csv-export" },
            { label: "Row Validation", slug: "extensions/csv-helper/row-validation" },
            { label: "Class Map", slug: "extensions/csv-helper/class-map" },
          ],
        },
        {
          label: "FAQ",
          items: [
            { label: "FAQ", slug: "faq" },
          ],
        },
      ],
    }),
    react(),
  ],

  vite: {
    plugins: [tailwindcss(), starlightThemeNord()],
  },
});
