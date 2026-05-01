// @ts-check
import { defineConfig } from "astro/config";
import starlight from "@astrojs/starlight";
import starlightThemeNord from "starlight-theme-nord";
import react from "@astrojs/react";
import tailwindcss from "@tailwindcss/vite";

// https://astro.build/config
export default defineConfig({
  integrations: [
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
        //
      ],
    }),
    react(),
  ],

  vite: {
    plugins: [tailwindcss(), starlightThemeNord()],
  },
});
