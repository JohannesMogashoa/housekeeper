import { z } from "zod";

export const householdNameSchema = z
  .string()
  .trim()
  .min(2, "Household name must contain at least 2 characters.")
  .max(120, "Household name cannot exceed 120 characters.");
