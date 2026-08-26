import type { Metadata } from "next";
import type { ReactNode } from "react";

import { TRPCProvider } from "@/lib/trpc/provider";

import "./globals.css";

export const metadata: Metadata = {
  title: "HouseKeeper",
  description: "Know what your home needs today.",
};

export default function RootLayout({ children }: Readonly<{ children: ReactNode }>) {
  return (
    <html lang="en">
      <body>
        <TRPCProvider>{children}</TRPCProvider>
      </body>
    </html>
  );
}
