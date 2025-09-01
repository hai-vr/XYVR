import Account from "./Account.jsx";

function Individual({ individual, index, isVisible = true }) {
    // Get all VRChat account links and filter to only show http/https URLs
    const vrChatLinks = individual.accounts
        ?.filter(account => account.namedApp === "VRChat" && account.specifics?.urls?.length > 0)
        ?.flatMap(account => account.specifics.urls)
        // Some users have links that are an empty string. We don't want this because clicking it causes the page to reload.
        // Generally, prevent links that aren't http:// nor https://
        ?.filter(url => url && (url.startsWith('http://') || url.startsWith('https://'))) || [];

    const copyToClipboard = async (url, event) => {
        event.stopPropagation(); // Prevent the container click event
        try {
            await navigator.clipboard.writeText(url);
        } catch (err) {
            
        }
    };

    return (
        <div style={{ 
            display: isVisible ? 'block' : 'none',
            background: 'white',
            border: '1px solid #e1e5e9',
            borderRadius: '12px',
            padding: '24px',
            boxShadow: '0 2px 8px rgba(0,0,0,0.06)',
            transition: 'transform 0.2s ease, box-shadow 0.2s ease',
            cursor: 'pointer'
        }}
            /*onMouseEnter={(e) => {
                e.target.style.transform = 'translateY(-2px)';
                e.target.style.boxShadow = '0 4px 16px rgba(0,0,0,0.12)';
            }}
            onMouseLeave={(e) => {
                e.target.style.transform = 'translateY(0)';
                e.target.style.boxShadow = '0 2px 8px rgba(0,0,0,0.06)';
            }}*/>
            <div style={{
                display: 'flex',
                alignItems: 'center',
                marginBottom: '16px',
                paddingBottom: '12px',
                borderBottom: '2px solid #f8f9fa'
            }}>
                <div style={{
                    width: '40px',
                    height: '40px',
                    borderRadius: '50%',
                    background: 'linear-gradient(135deg, #4facfe 0%, #00f2fe 100%)',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    color: 'white',
                    fontSize: '18px',
                    fontWeight: 'bold',
                    marginRight: '12px'
                }}>
                    {individual.displayName?.charAt(0).toUpperCase() || '?'}
                </div>
                <h3 style={{
                    margin: 0,
                    fontSize: '20px',
                    fontWeight: '600',
                    color: '#2c3e50'
                }}>
                    {individual.displayName}
                </h3>
                {individual.isAnyContact && (
                    <span style={{
                        marginLeft: '12px',
                        background: '#e8f5e8',
                        color: '#27ae60',
                        padding: '4px 8px',
                        borderRadius: '12px',
                        fontSize: '12px',
                        fontWeight: '500'
                    }}>
                        📞 Contact
                    </span>
                )}
            </div>

            <div style={{ marginBottom: '20px' }}>
                {individual.accounts && individual.accounts.length > 0 ? (
                    <div style={{
                        display: 'grid',
                        gap: '8px',
                        gridTemplateColumns: 'repeat(auto-fit, minmax(280px, 1fr))'
                    }}>
                        {individual.accounts.map((account, accountIndex) => (
                            <Account key={accountIndex} account={account} />
                        ))}
                    </div>
                ) : (
                    <div style={{
                        textAlign: 'center',
                        padding: '20px',
                        color: '#6c757d',
                        fontStyle: 'italic',
                        background: '#f8f9fa',
                        borderRadius: '8px',
                        border: '2px dashed #dee2e6'
                    }}>
                        📭 No accounts found
                    </div>
                )}
            </div>

            {vrChatLinks.length > 0 && (
                <div style={{
                    background: '#f0f8ff',
                    border: '1px solid #b3d9ff',
                    borderRadius: '8px',
                    padding: '12px',
                    marginBottom: '16px'
                }}>
                    <div style={{
                        fontSize: '14px',
                        color: '#1976d2',
                        fontWeight: '600',
                        marginBottom: '8px',
                        display: 'flex',
                        alignItems: 'center'
                    }}>
                        💬 VRChat Links:
                    </div>
                    <div style={{
                        display: 'flex',
                        flexDirection: 'column',
                        gap: '6px'
                    }}>
                        {vrChatLinks.map((url, linkIndex) => (
                            <div 
                                key={linkIndex}
                                style={{
                                    background: '#e3f2fd',
                                    border: '1px solid #bbdefb',
                                    borderRadius: '4px',
                                    padding: '6px 12px',
                                    display: 'flex',
                                    alignItems: 'center',
                                    justifyContent: 'space-between',
                                    maxWidth: '100%',
                                    overflow: 'hidden',
                                    transition: 'background-color 0.2s ease'
                                }}
                                onMouseEnter={(e) => {
                                    e.target.style.backgroundColor = '#bbdefb';
                                }}
                                onMouseLeave={(e) => {
                                    e.target.style.backgroundColor = '#e3f2fd';
                                }}
                            >
                                <a
                                    href={url}
                                    rel="noopener noreferrer"
                                    style={{
                                        color: '#1976d2',
                                        textDecoration: 'none',
                                        fontSize: '13px',
                                        overflow: 'hidden',
                                        textOverflow: 'ellipsis',
                                        whiteSpace: 'nowrap',
                                        marginRight: '8px',
                                        flex: 1,
                                        cursor: 'pointer'
                                    }}
                                    onMouseEnter={(e) => {
                                        e.target.style.textDecoration = 'underline';
                                    }}
                                    onMouseLeave={(e) => {
                                        e.target.style.textDecoration = 'none';
                                    }}
                                >
                                    🔗 {url}
                                </a>
                                <button
                                    onClick={(e) => copyToClipboard(url, e)}
                                    style={{
                                        background: 'transparent',
                                        border: 'none',
                                        color: '#1976d2',
                                        cursor: 'pointer',
                                        fontSize: '11px',
                                        opacity: 0.7,
                                        padding: '4px 8px',
                                        borderRadius: '4px',
                                        transition: 'all 0.2s ease',
                                        flexShrink: 0
                                    }}
                                    onMouseEnter={(e) => {
                                        e.target.style.opacity = '1';
                                        e.target.style.backgroundColor = 'rgba(25, 118, 210, 0.1)';
                                    }}
                                    onMouseLeave={(e) => {
                                        e.target.style.opacity = '0.7';
                                        e.target.style.backgroundColor = 'transparent';
                                    }}
                                    title="Copy link to clipboard"
                                >
                                    📋 Copy
                                </button>
                            </div>
                        ))}
                    </div>
                </div>
            )}
        </div>
    );
}

export default Individual;