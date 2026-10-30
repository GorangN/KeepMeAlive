export function resolveTarget(config, target) {
  if (!['development', 'staging', 'production'].includes(target)) {
    throw new Error('Choose development, staging or production.');
  }
  const branch = config[target];
  const projectId = branch?.projectId ?? config.projectId;
  if (!branch?.branchId || !projectId) { throw new Error('The target project and branch must be configured.'); }
  return { branch, projectId };
}
