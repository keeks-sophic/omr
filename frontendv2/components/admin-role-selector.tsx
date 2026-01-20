"use client";

const ALL_ROLES = ["Admin", "Operator", "Viewer", "Pending"] as const;

export default function AdminRoleSelector(props: {
  value: string[];
  onChange: (roles: string[]) => void;
  disabled?: boolean;
}) {
  const selected = new Set(props.value);

  return (
    <div className="flex flex-wrap gap-3">
      {ALL_ROLES.map((role) => {
        const checked = selected.has(role);
        return (
          <label key={role} className="flex items-center gap-2 text-sm">
            <input
              type="checkbox"
              checked={checked}
              disabled={props.disabled}
              onChange={(e) => {
                const next = new Set(props.value);
                if (e.target.checked) next.add(role);
                else next.delete(role);
                props.onChange(Array.from(next));
              }}
            />
            <span>{role}</span>
          </label>
        );
      })}
    </div>
  );
}

