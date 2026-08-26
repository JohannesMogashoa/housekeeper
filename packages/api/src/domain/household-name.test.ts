import { describe, expect, it } from "vitest";

import { householdNameSchema } from "./household-name";

describe("householdNameSchema", () => {
  it("trims a valid household name", () => {
    expect(householdNameSchema.parse("  Mogashoa Home  ")).toBe("Mogashoa Home");
  });

  it("rejects names shorter than two characters", () => {
    expect(() => householdNameSchema.parse(" A ")).toThrow(
      "Household name must contain at least 2 characters.",
    );
  });

  it("rejects names longer than 120 characters", () => {
    expect(() => householdNameSchema.parse("x".repeat(121))).toThrow(
      "Household name cannot exceed 120 characters.",
    );
  });
});
