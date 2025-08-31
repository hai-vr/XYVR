import Account from "./Account.jsx";

function Individual({ individual, index }) {
    return (
        <div key={index} style={{
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

            <div>
                <div style={{
                    display: 'flex',
                    alignItems: 'center',
                    marginBottom: '12px'
                }}>
                    <h4 style={{
                        margin: 0,
                        fontSize: '16px',
                        fontWeight: '600',
                        color: '#34495e'
                    }}>
                        🎮 Accounts ({individual.accounts?.length || 0})
                    </h4>
                </div>

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
        </div>
    );
}

export default Individual;